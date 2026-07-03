using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shoppy.Business.Auth.DataTransferObjects;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Services;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;

namespace Shoppy.Business.Auth;

public sealed class AuthService(UserManager<User> _userManager, JwtProvider _jwtProvider, ApplicationDbContext _context, IEmailService _emailService, ILogger<AuthService> _logger) : IAuthService
{


    // LOGIN
    public async Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(request.UserName);

        if (user is null)
        {
            _logger.LogWarning("Login failed: unknown username {UserName}", request.UserName);
            return Result<LoginResponseDto>.Failure(401, ErrorMessages.Auth.InvalidCredentials);
        }

        if (user.IsDeleted)
        {
            _logger.LogWarning("Login failed: account {UserId} is deactivated", user.Id);
            return Result<LoginResponseDto>.Failure(401, ErrorMessages.Auth.InvalidCredentials);
        }



        var result = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!result)
        {
            _logger.LogWarning("Login failed: incorrect password for user {UserId}", user.Id);
            return Result<LoginResponseDto>.Failure(401, ErrorMessages.Auth.InvalidCredentials);
        }


        var roles = await _context.AppUserRoles
              .Where(u => u.UserId == user.Id)
              .LeftJoin(_context.AppRoles, m => m.RoleId, m => m.Id, (userRole, role) => role)
              .Select(s => (string?)s!.Name)
              .ToListAsync(cancellationToken);

        var roleIds = await _context.AppUserRoles
              .Where(u => u.UserId == user.Id)
              .Select(u => u.RoleId)
              .ToListAsync(cancellationToken);

        var permissions = await _context.RolePermissions
              .Where(rp => roleIds.Contains(rp.RoleId))
              .Select(rp => rp.PermissionName)
              .Distinct()
              .ToListAsync(cancellationToken);

        var loginResponse = _jwtProvider.CreateToken(user, roles, permissions);

        // Save refresh token to database — login always starts a brand new token family
        var refreshToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            Token = loginResponse.RefreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow,
            IsRevoked = false,
            FamilyId = Guid.CreateVersion7()
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} logged in, started refresh token family {FamilyId}", user.Id, refreshToken.FamilyId);

        return loginResponse;
    }

    public async Task<Result<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        // Find the refresh token
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (storedToken is null)
        {
            _logger.LogWarning("Refresh token attempt failed: token not found");
            return Result<LoginResponseDto>.Failure(401, ErrorMessages.Auth.InvalidRefreshToken);
        }

        // check if token is revoked — reuse of an already-revoked token is a replay/theft signal,
        // so the entire token family is revoked rather than just rejecting this one request.
        if (storedToken.IsRevoked)
        {
            var familyTokens = await _context.RefreshTokens
                .Where(rt => rt.FamilyId == storedToken.FamilyId && !rt.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var t in familyTokens)
                t.IsRevoked = true;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("Refresh token reuse detected — revoking entire token family {FamilyId} for user {UserId}", storedToken.FamilyId, storedToken.UserId);

            return Result<LoginResponseDto>.Failure(401, ErrorMessages.Auth.RefreshTokenFamilyRevoked);
        }

        // check if token is expired
        if (storedToken.ExpiresAt < DateTimeOffset.UtcNow)
        {
            _logger.LogWarning("Refresh token attempt failed: token expired for user {UserId}", storedToken.UserId);
            return Result<LoginResponseDto>.Failure(401, ErrorMessages.Auth.RefreshTokenExpired);
        }

        // check if user is deleted
        if (storedToken.User.IsDeleted)
        {
            _logger.LogWarning("Refresh token attempt failed: account {UserId} is deactivated", storedToken.UserId);
            return Result<LoginResponseDto>.Failure(401, ErrorMessages.Auth.AccountDeactivated);
        }


        // get user roles
        var roles = await _context.AppUserRoles
            .Where(u => u.UserId == storedToken.UserId)
            .LeftJoin(_context.AppRoles, m => m.RoleId, m => m.Id, (userRole, role) => role)
            .Select(s => (string?)s!.Name)
            .ToListAsync(cancellationToken);

        // get user permissions (via role ids, from RolePermissions)
        var roleIds = await _context.AppUserRoles
            .Where(u => u.UserId == storedToken.UserId)
            .Select(u => u.RoleId)
            .ToListAsync(cancellationToken);

        var permissions = await _context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.PermissionName)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Generate new tokens
        var loginResponse = _jwtProvider.CreateToken(storedToken.User, roles, permissions);

        // Save new refresh token — carries the same FamilyId forward (rotation, not a new family)
        var newRefreshToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = storedToken.UserId,
            Token = loginResponse.RefreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow,
            IsRevoked = false,
            FamilyId = storedToken.FamilyId
        };

        // revoke old token, linking it to the token that replaced it
        storedToken.IsRevoked = true;
        storedToken.ReplacedByToken = newRefreshToken.Token;

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Refresh token rotated for user {UserId} in family {FamilyId}", storedToken.UserId, storedToken.FamilyId);

        return loginResponse;
    }


    // FORGOT PASSWORD
    public async Task<Result<string>> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        // Always return success to prevent User enumeration attacks.
        // do not reveal whether the email is registered or not.

        if (user is null || user.IsDeleted)
            return Result<string>.Success("If this email is registered, a password reset code has been sent.");
        user.GeneratePasswordResetCode();
        await _userManager.UpdateAsync(user);

        string emailBody = $@"
            <h2>Password Reset Request</h2>
            <p>You have requested to reset your password. Please use the 6-digit code below.</p>
            <h3 style='letter-spacing: 8px; font-size: 28px; color: #2563EB;'><strong>{user.PasswordResetCode}</strong></h3>
            <p>This code will expire in <strong>15 minutes</strong>. Do not share it with anyone.</p>
            <hr/>
            <p style='font-size: 12px; color: #6B7280;'>If you did not request a password reset, you can safely ignore this email.</p>";

        try
        {
            await _emailService.SendEmailAsync(user.Email!, "Password reset code", emailBody, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to user {UserId}", user.Id);

            user.ClearPasswordResetCode();
            await _userManager.UpdateAsync(user);
            return Result<string>.Failure(500, ErrorMessages.Auth.EmailSendFailure);
        }

        return Result<string>.Success("If this email is registered, a password reset code has been sent.");
    }

    public async Task<Result<string>> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null || user.IsDeleted)
            return Result<string>.Failure(404, ErrorMessages.User.NotFound);

        if (string.IsNullOrEmpty(user.PasswordResetCode) || user.PasswordResetCode != request.Code)
            return Result<string>.Failure(400, ErrorMessages.Auth.InvalidResetCode);

        if (user.PasswordResetCodeExpires is null || user.PasswordResetCodeExpires < DateTimeOffset.UtcNow)
            return Result<string>.Failure(400, ErrorMessages.Auth.InvalidResetCode);

        // change password directly without identity's token provider system since we have our own OTP verification above
        var removeResult = await _userManager.RemovePasswordAsync(user);

        if (!removeResult.Succeeded)
            return Result<string>.Failure(400, string.Join(", ", removeResult.Errors.Select(e => e.Description)));

        var addResult = await _userManager.AddPasswordAsync(user, request.NewPassword);

        if (!addResult.Succeeded)
            return Result<string>.Failure(400, string.Join(", ", addResult.Errors.Select(e => e.Description)));

        user.ClearPasswordResetCode();
        await _userManager.UpdateAsync(user);

        return Result<string>.Success("Password has been reset successfully.");
    }

}

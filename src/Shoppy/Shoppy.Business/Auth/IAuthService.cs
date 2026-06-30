using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shoppy.Business.Auth.DataTransferObjects;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Services;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;

namespace Shoppy.Business.Auth;

public interface IAuthService
{
    Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);

    Task<Result<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken);

    Task<Result<string>> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken);

    Task<Result<string>> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken);
}

public sealed class AuthService(UserManager<User> _userManager, JwtProvider _jwtProvider, ApplicationDbContext _context, IEmailService _emailService) : IAuthService
{


    // LOGIN
    public async Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(request.UserName);

        if (user is null)
            return Result<LoginResponseDto>.Failure(401, "User name or password is incorrect.");

        if (user.IsDeleted)
            return Result<LoginResponseDto>.Failure(401, "User name or password is incorrect.");



        var result = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!result)
            return Result<LoginResponseDto>.Failure(401, "User name or password is incorrect.");


        var roles = await _context.AppUserRoles
              .Where(u => u.UserId == user.Id)
              .LeftJoin(_context.AppRoles, m => m.RoleId, m => m.Id, (userRole, role) => role)
              .Select(s => s!.Name)
              .ToListAsync(cancellationToken);


        var loginResponse = _jwtProvider.CreateToken(user, roles);

        // Save refresh token to database
        var refreshToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            Token = loginResponse.RefreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow,
            IsRevoked = false
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return loginResponse;
    }

    public async Task<Result<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        // Find the refresh token
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (storedToken is null)
            return Result<LoginResponseDto>.Failure(401, "Invalid refresh token.");

        // check if token is revoked
        if (storedToken.IsRevoked)
            return Result<LoginResponseDto>.Failure(401, "Refresh token has been revoked.");

        // check if token is expired
        if (storedToken.ExpiresAt < DateTimeOffset.UtcNow)
            return Result<LoginResponseDto>.Failure(401, "Refresh token has expired.");

        // check if user is deleted
        if (storedToken.User.IsDeleted)
            return Result<LoginResponseDto>.Failure(401, "User account is deactivated");

        // revoke old token 
        storedToken.IsRevoked = true;


        // get user roles
        var roles = await _context.AppUserRoles
            .Where(u => u.UserId == storedToken.UserId)
            .LeftJoin(_context.AppRoles, m => m.RoleId, m => m.Id, (userRole, role) => role)
            .Select(s => s!.Name)
            .ToListAsync(cancellationToken);

        // Generate new tokens
        var loginResponse = _jwtProvider.CreateToken(storedToken.User, roles);

        // Save new refresh token
        var newRefreshToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = storedToken.UserId,
            Token = loginResponse.RefreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow,
            IsRevoked = false
        };

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync(cancellationToken);

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
        catch (Exception)
        {

            user.ClearPasswordResetCode();
            await _userManager.UpdateAsync(user);
            return Result<string>.Failure(500, "Failed to sent reset password reset email. Please try again later.");
        }

        return Result<string>.Success("If this email is registered, a password reset code has been sent.");
    }

    public async Task<Result<string>> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null || user.IsDeleted)
            return Result<string>.Failure(404, "User not found.");

        if (string.IsNullOrEmpty(user.PasswordResetCode) || user.PasswordResetCode != request.Code)
            return Result<string>.Failure(400, "Invalid reset code.");

        // change password directly without identity's token provider system since we have our own OTP verification above
        var removeResult = await _userManager.RemovePasswordAsync(user);

        if (!removeResult.Succeeded)
            return Result<string>.Failure(400, string.Join(", ", removeResult.Errors.Select(e => e.Description)));

        var addResult = await _userManager.AddPasswordAsync(user, request.NewPassword);

        if (!addResult.Succeeded)
            return Result<string>.Failure(400, string.Join(", ", removeResult.Errors.Select(e => e.Description)));

        return Result<string>.Success("Password has been reset successfully.");
    }

}
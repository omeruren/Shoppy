using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shoppy.Business.Auth.DataTransferObjects;
using Shoppy.Business.BaseResult;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;

namespace Shoppy.Business.Auth;

public interface IAuthService
{
    Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);

    Task<Result<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken);
}

public sealed class AuthService(UserManager<User> _userManager, JwtProvider _jwtProvider, ApplicationDbContext _context) : IAuthService
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
}
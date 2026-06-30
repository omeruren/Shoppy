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


        var accessToken = _jwtProvider.CreateToken(user, roles);

        return Result<LoginResponseDto>.Success(accessToken);
    }
}
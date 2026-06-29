using Microsoft.AspNetCore.Identity;
using Shoppy.Business.Auth.DataTransferObjects;
using Shoppy.Business.BaseResult;
using Shoppy.Entity.Models;

namespace Shoppy.Business.Auth;

public interface IAuthService
{
    Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request);
}

public sealed class AuthService(UserManager<User> _userManager, JwtProvider _jwtProvider) : IAuthService
{

    // LOGIN
    public async Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var user = await _userManager.FindByNameAsync(request.UserName);

        if (user is null)
            return Result<LoginResponseDto>.Failure(401, "User name or password is incorrect.");

        if (user.IsDeleted)
            return Result<LoginResponseDto>.Failure(401, "User name or password is incorrect.");



        var result = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!result)
            return Result<LoginResponseDto>.Failure(401, "User name or password is incorrect.");

        var accessToken = _jwtProvider.CreateToken(user);

        return Result<LoginResponseDto>.Success(accessToken);
    }
}
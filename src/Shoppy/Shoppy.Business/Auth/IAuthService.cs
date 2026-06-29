using Microsoft.AspNetCore.Identity;
using Shoppy.Business.BaseResult;
using Shoppy.Entity.Models;

namespace Shoppy.Business.Auth;

public interface IAuthService
{
    Task<Result<string>> LoginAsync(string userName, string password);
}

public sealed class AuthService(UserManager<User> _userManager, JwtProvider _jwtProvider) : IAuthService
{

    // LOGIN
    public async Task<Result<string>> LoginAsync(string userName, string password)
    {
        var user = await _userManager.FindByNameAsync(userName);

        if (user is null)
            return Result<string>.Failure(401, "User name or password is incorrect.");

        if (user.IsDeleted)
            return Result<string>.Failure(401, "User name or password is incorrect.");



        var result = await _userManager.CheckPasswordAsync(user, password);

        if (!result)
            return Result<string>.Failure(401, "User name or password is incorrect.");

        var token = _jwtProvider.CreateToken(user);

        return Result<string>.Success(token);
    }
}
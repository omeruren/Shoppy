using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Users.DataTransferObjects;
using Shoppy.Entity.Models;

namespace Shoppy.Business.Users;

public sealed class UserService(UserManager<User> _userManager) : IUserService
{


    // GET ALL USERS

    public async Task<Result<List<User>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var result = await _userManager
            .Users
            .AsNoTracking()
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);

        return result;
    }


    // GET USER BY ID

    public async Task<Result<User>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        User? user = await _userManager.FindByIdAsync(id.ToString());

        if (user is null)
            return Result<User>.Failure(404, "User not found.");

        return user;
    }


    // CREATE USER


    public async Task<Result<string>> CreateAsync(UserCreateDto request, CancellationToken cancellationToken)
    {
        bool isEmailExists = await _userManager.Users.AnyAsync(u => u.Email == request.Email, cancellationToken);

        if (isEmailExists)
            return Result<string>.Failure(409, "This email is already taken by someone else.");

        User user = User.Create(
            request.FirstName,
            request.LastName,
            request.UserName,
            request.Email);

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return Result<string>.Failure(400, result.Errors.Select(e => e.Description).ToList());
        return "User created successfully.";
    }


    // UPDATE USER

    public async Task<Result<string>> UpdateAsync(UserUpdateDto request, CancellationToken cancellationToken)
    {
        User? user = await _userManager.FindByIdAsync(request.Id.ToString());

        if (user is null)
            return Result<string>.Failure(404, "User not found.");

        user.Update(
            request.FirstName,
            request.LastName,
            request.UserName,
            request.Email);

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return Result<string>.Failure(400, result.Errors.Select(e => e.Description).ToList());

        return "User updated successfully.";
    }


    // DELETE USER

    public async Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user is null)
            return Result<string>.Failure(404, "User not found.");

        if (!user.IsDeleted)
        {
            user.Delete();
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return Result<string>.Failure(400, result.Errors.Select(e => e.Description).ToList());

        }
        else
        {
            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
                return Result<string>.Failure(400, result.Errors.Select(e => e.Description).ToList());
        }

        return "User deleted.";
    }


}
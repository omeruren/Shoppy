using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Extensions;
using Shoppy.Business.Users.DataTransferObjects;
using Shoppy.Entity.Models;

namespace Shoppy.Business.Users;

public sealed class UserService(UserManager<User> _userManager) : IUserService
{
    // ── Projection helper ────────────────────────────────────────────────

    private static UserProfileDto ToProfile(User u) => new(
        u.Id, u.FirstName, u.LastName, u.FullName, u.UserName!, u.Email!);

    // ── Admin: GET ALL ────────────────────────────────────────────────────

    public async Task<Result<PaginationResultDto<UserProfileDto>>> GetAllAsync(
        PaginationRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _userManager.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.FullName)
            .Select(u => new UserProfileDto(u.Id, u.FirstName, u.LastName, u.FullName, u.UserName!, u.Email!))
            .WithPagination(request, cancellationToken);

        return result;
    }

    // ── Admin: GET BY ID ─────────────────────────────────────────────────

    public async Task<Result<UserProfileDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        User? user = await _userManager.FindByIdAsync(id.ToString());

        if (user is null || user.IsDeleted)
            return Result<UserProfileDto>.Failure(404, ErrorMessages.User.NotFound);

        return ToProfile(user);
    }

    // ── Admin: CREATE ────────────────────────────────────────────────────

    public async Task<Result<string>> CreateAsync(UserCreateDto request, CancellationToken cancellationToken)
    {
        bool isEmailExists = await _userManager.Users
            .AnyAsync(u => u.Email == request.Email, cancellationToken);

        if (isEmailExists)
            return Result<string>.Failure(409, ErrorMessages.User.EmailAlreadyTaken);

        User user = User.Create(request.FirstName, request.LastName, request.UserName, request.Email);

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return Result<string>.Failure(400, result.Errors.Select(e => e.Description).ToList());

        return Result<string>.Success("User created successfully.", 201);
    }

    // ── Admin: UPDATE ────────────────────────────────────────────────────

    public async Task<Result<string>> UpdateAsync(UserUpdateDto request, CancellationToken cancellationToken)
    {
        User? user = await _userManager.FindByIdAsync(request.Id.ToString());

        if (user is null || user.IsDeleted)
            return Result<string>.Failure(404, ErrorMessages.User.NotFound);

        user.Update(request.FirstName, request.LastName, request.UserName, request.Email);

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return Result<string>.Failure(400, result.Errors.Select(e => e.Description).ToList());

        return "User updated successfully.";
    }

    // ── Admin: DELETE ────────────────────────────────────────────────────

    public async Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user is null)
            return Result<string>.Failure(404, ErrorMessages.User.NotFound);

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

    // ── Self-service: GET MY PROFILE ─────────────────────────────────────

    public async Task<Result<UserProfileDto>> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        User? user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null || user.IsDeleted)
            return Result<UserProfileDto>.Failure(404, ErrorMessages.User.NotFound);

        return ToProfile(user);
    }

    // ── Self-service: UPDATE MY PROFILE ──────────────────────────────────

    public async Task<Result<string>> UpdateSelfAsync(
        Guid userId, UserUpdateSelfDto request, CancellationToken cancellationToken)
    {
        User? user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null || user.IsDeleted)
            return Result<string>.Failure(404, ErrorMessages.User.NotFound);

        // Only name and username are self-editable; email changes require admin
        user.Update(request.FirstName, request.LastName, request.UserName, user.Email!);

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return Result<string>.Failure(400, result.Errors.Select(e => e.Description).ToList());

        return "Profile updated successfully.";
    }

    // ── Self-service: CHANGE MY PASSWORD ─────────────────────────────────

    public async Task<Result<string>> ChangePasswordAsync(
        Guid userId, ChangePasswordDto request, CancellationToken cancellationToken)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
            return Result<string>.Failure(400, ErrorMessages.Auth.PasswordMismatch);

        User? user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null || user.IsDeleted)
            return Result<string>.Failure(404, ErrorMessages.User.NotFound);

        var result = await _userManager.ChangePasswordAsync(
            user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
            return Result<string>.Failure(400, result.Errors.Select(e => e.Description).ToList());

        return "Password changed successfully.";
    }
}
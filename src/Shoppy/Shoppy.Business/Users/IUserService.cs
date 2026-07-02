using Shoppy.Business.BaseResult;
using Shoppy.Business.Extensions;
using Shoppy.Business.Users.DataTransferObjects;

namespace Shoppy.Business.Users;

public interface IUserService
{
    // ── Admin operations ──────────────────────────────────────────────────
    Task<Result<PaginationResultDto<UserProfileDto>>> GetAllAsync(PaginationRequestDto request, CancellationToken cancellationToken);
    Task<Result<UserProfileDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<string>> CreateAsync(UserCreateDto request, CancellationToken cancellationToken);
    Task<Result<string>> UpdateAsync(UserUpdateDto request, CancellationToken cancellationToken);
    Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken);

    // ── Self-service operations ───────────────────────────────────────────
    Task<Result<UserProfileDto>> GetProfileAsync(Guid userId, CancellationToken cancellationToken);
    Task<Result<string>> UpdateSelfAsync(Guid userId, UserUpdateSelfDto request, CancellationToken cancellationToken);
    Task<Result<string>> ChangePasswordAsync(Guid userId, ChangePasswordDto request, CancellationToken cancellationToken);
}
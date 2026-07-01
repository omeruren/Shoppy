using Shoppy.Business.BaseResult;
using Shoppy.Business.Extensions;
using Shoppy.Business.Users.DataTransferObjects;
using Shoppy.Entity.Models;

namespace Shoppy.Business.Users;

public interface IUserService
{
    Task<Result<PaginationResultDto<User>>> GetAllAsync(PaginationRequestDto request, CancellationToken cancellationToken);
    Task<Result<User>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<string>> CreateAsync(UserCreateDto request, CancellationToken cancellationToken);
    Task<Result<string>> UpdateAsync(UserUpdateDto request, CancellationToken cancellationToken);
    Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken);

}
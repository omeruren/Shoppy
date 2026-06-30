using Shoppy.Business.BaseResult;
using Shoppy.Business.UserRoles.DataTransferObjects;
using Shoppy.Entity.Models;

namespace Shoppy.Business.UserRoles;

public interface IUserRoleService
{
    Task<Result<List<UserRole>>> GetAllAsync(CancellationToken cancellationToken);
    Task<Result<string>> CreateAsync(UserRoleCreateDto request, CancellationToken cancellationToken);

    Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

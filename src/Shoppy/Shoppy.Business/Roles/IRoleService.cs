using Shoppy.Business.BaseResult;
using Shoppy.Business.Extensions;
using Shoppy.Business.Roles.DataTransferObjects;

namespace Shoppy.Business.Roles;

public interface IRoleService
{
    Task<Result<PaginationResultDto<RoleResultDto>>> GetAllAsync(PaginationRequestDto request, CancellationToken cancellationToken);
    Task<Result<RoleResultDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<string>> CreateAsync(RoleCreateDto request, CancellationToken cancellationToken);
    Task<Result<string>> UpdateAsync(RoleUpdateDto request, CancellationToken cancellationToken);
    Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<List<string>>> GetPermissionsAsync(Guid roleId, CancellationToken cancellationToken);
    Task<Result<string>> UpdatePermissionsAsync(Guid roleId, List<string> permissions, CancellationToken cancellationToken);
}

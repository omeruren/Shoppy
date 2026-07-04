using Shoppy.Business.BaseResult;

namespace Shoppy.Business.Roles.DataTransferObjects;

public sealed class RoleResultDto : BaseEntityDto
{
    public string Name { get; set; } = default!;
    public byte[]? RowVersion { get; set; }
}

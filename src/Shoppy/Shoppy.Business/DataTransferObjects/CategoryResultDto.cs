using Shoppy.Business.DataTransferObjects.Base;

namespace Shoppy.Business.DataTransferObjects;

public sealed class CategoryResultDto : BaseEntityDto
{
    public string Name { get; set; } = default!;
}
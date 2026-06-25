using Shoppy.Business.BaseResult;

namespace Shoppy.Business.Categories.DataTransferObjects;

public sealed class CategoryResultDto : BaseEntityDto
{
    public string Name { get; set; } = default!;
}
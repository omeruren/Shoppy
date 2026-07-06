using Shoppy.Business.BaseResult;

namespace Shoppy.Business.Products.DataTransferObjects;

public sealed class ProductResultDto : BaseEntityDto
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = default!;
    public byte[]? RowVersion { get; set; }
}

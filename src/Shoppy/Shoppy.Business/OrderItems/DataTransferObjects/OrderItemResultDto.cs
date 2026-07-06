using Shoppy.Business.BaseResult;

namespace Shoppy.Business.OrderItems.DataTransferObjects;

public sealed class OrderItemResultDto : BaseEntityDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public byte[]? RowVersion { get; set; }
}

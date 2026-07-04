using Shoppy.Business.BaseResult;

namespace Shoppy.Business.OrderItems.DataTransferObjects;

public sealed class OrderItemResultDto : BaseEntityDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public byte[]? RowVersion { get; set; }
}

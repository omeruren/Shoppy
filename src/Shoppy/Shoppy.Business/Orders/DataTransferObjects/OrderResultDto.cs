using Shoppy.Business.BaseResult;
using Shoppy.Business.OrderItems.DataTransferObjects;

namespace Shoppy.Business.Orders.DataTransferObjects;

public sealed class OrderResultDto : BaseEntityDto
{
    public DateTimeOffset OrderDate { get; set; }
    public ICollection<OrderItemResultDto> Items { get; set; } = [];
    public byte[]? RowVersion { get; set; }
}

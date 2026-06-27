using Shoppy.Business.OrderItems.DataTransferObjects;

namespace Shoppy.Business.Orders.DataTransferObjects;

public sealed record OrderCreateDto(ICollection<OrderItemCreateDto> Items);

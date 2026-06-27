using Shoppy.Business.OrderItems.DataTransferObjects;

namespace Shoppy.Business.Orders.DataTransferObjects;

public sealed record OrderUpdateDto(Guid Id, DateTimeOffset OrderDate, ICollection<OrderItemUpdateDto> Items);

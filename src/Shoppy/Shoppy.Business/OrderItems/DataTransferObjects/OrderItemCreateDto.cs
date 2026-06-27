namespace Shoppy.Business.OrderItems.DataTransferObjects;

public sealed record OrderItemCreateDto(Guid ProductId, int Quantity);

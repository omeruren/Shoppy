namespace Shoppy.Business.OrderItems.DataTransferObjects;

public sealed record OrderItemUpdateDto(Guid Id, Guid ProductId, int Quantity, byte[]? RowVersion = null);

namespace Shoppy.Business.Products.DataTransferObjects;

public sealed record ProductUpdateDto(Guid Id, string Name, string? Description, decimal Price, Guid CategoryId, byte[]? RowVersion = null);

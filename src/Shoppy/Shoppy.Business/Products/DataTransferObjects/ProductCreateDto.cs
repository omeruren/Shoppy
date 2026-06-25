namespace Shoppy.Business.Products.DataTransferObjects;

public sealed record ProductCreateDto(string Name, string? Description, decimal Price, Guid CategoryId);

using Shoppy.Entity.Abstraction;

namespace Shoppy.Entity.Models;

public class Product : BaseEntity
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = default!;

    public ICollection<OrderItem> OrderItems { get; set; } = [];
}

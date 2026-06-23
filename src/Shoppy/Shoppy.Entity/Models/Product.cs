using Shoppy.Entity.Abstraction;

namespace Shoppy.Entity.Models;

public class Product : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Price { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = default!;
}

using Shoppy.Entity.Abstraction;

namespace Shoppy.Entity.Models;

public sealed class Category : BaseEntity
{
    public string Name { get; set; } = default!;
    public ICollection<Product> Products { get; set; } = [];
}

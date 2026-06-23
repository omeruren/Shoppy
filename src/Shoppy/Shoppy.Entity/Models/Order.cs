using Shoppy.Entity.Abstraction;

namespace Shoppy.Entity.Models;

public sealed class Order : BaseEntity
{
    public DateTimeOffset OrderDate { get; set; }
    public int Quantity { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;
}

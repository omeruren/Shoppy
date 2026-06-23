using Shoppy.Entity.Abstraction;

namespace Shoppy.Entity.Models;

public sealed class Order : BaseEntity
{
    public DateTimeOffset OrderDate { get; set; }

    public ICollection<OrderItem> Items { get; set; } = default!;
}

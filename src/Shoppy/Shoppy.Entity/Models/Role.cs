using Shoppy.Entity.Abstraction;

namespace Shoppy.Entity.Models;

public sealed class Role : BaseEntity
{
    public string Name { get; set; } = default!;
}

public sealed class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}

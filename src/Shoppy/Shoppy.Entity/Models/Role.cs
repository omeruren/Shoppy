using Shoppy.Entity.Abstraction;

namespace Shoppy.Entity.Models;

public sealed class Role : BaseEntity
{
    public string Name { get; set; } = default!;

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}

public sealed class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}

/// <summary>
/// Maps a custom Role to a static permission string (e.g. "Users.Read").
/// Permissions are static constants — no Permission table needed.
/// </summary>
public sealed class RolePermission
{
    public Guid   RoleId         { get; set; }
    public string PermissionName { get; set; } = default!;

    public Role Role { get; set; } = default!;
}

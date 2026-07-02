using Microsoft.EntityFrameworkCore;
using Shoppy.Business.Permissions;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;

namespace Shoppy.WebAPI.Seed;

/// <summary>
/// Ensures the built-in Admin/Customer roles exist and carry the permission
/// set defined in Permissions.GetAdminPermissions()/GetCustomerPermissions().
/// Runs once at startup; does not touch user-role assignments.
/// </summary>
internal static class RolePermissionSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await EnsureRoleAsync(context, "Admin", Permissions.GetAdminPermissions());
        await EnsureRoleAsync(context, "Customer", Permissions.GetCustomerPermissions());

        await context.SaveChangesAsync();
    }

    private static async Task EnsureRoleAsync(ApplicationDbContext context, string roleName, IReadOnlyList<string> permissions)
    {
        var role = await context.AppRoles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Name == roleName);

        if (role is null)
        {
            role = new Role { Name = roleName };
            context.AppRoles.Add(role);
        }

        var existingPermissions = role.RolePermissions.Select(rp => rp.PermissionName).ToHashSet(StringComparer.Ordinal);

        foreach (var permission in permissions)
        {
            if (existingPermissions.Add(permission))
                role.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionName = permission });
        }
    }
}

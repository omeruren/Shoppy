using Microsoft.AspNetCore.Authorization;

namespace Shoppy.Business.Permissions;

/// <summary>
/// Authorization requirement that demands a specific permission claim
/// to be present in the JWT token.
/// </summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

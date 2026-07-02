using Microsoft.AspNetCore.Authorization;

namespace Shoppy.Business.Permissions;

/// <summary>
/// Handles PermissionRequirement by checking whether the current user's
/// JWT contains a "permission" claim that matches the required permission.
/// </summary>
public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    public const string PermissionClaimType = "permission";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // User must be authenticated
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        // Check for a matching "permission" claim in the token
        var hasPermission = context.User.Claims
            .Any(c => c.Type == PermissionClaimType
                   && c.Value.Equals(requirement.Permission, StringComparison.OrdinalIgnoreCase));

        if (hasPermission)
            context.Succeed(requirement);
        else
            context.Fail();

        return Task.CompletedTask;
    }
}

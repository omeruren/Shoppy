using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Shoppy.Business.Permissions;
using System.Security.Claims;

namespace Shoppy.UnitTests.Services;

public class PermissionAuthorizationHandlerTests
{
    private readonly PermissionAuthorizationHandler _handler = new();

    private static AuthorizationHandlerContext CreateContext(
        PermissionRequirement requirement, ClaimsPrincipal user) =>
        new([requirement], user, resource: null);

    [Fact]
    public async Task HandleAsync_Should_Succeed_When_User_Has_Matching_Permission_Claim()
    {
        var requirement = new PermissionRequirement("Users.Read");
        var identity = new ClaimsIdentity(
            [new Claim(PermissionAuthorizationHandler.PermissionClaimType, "Users.Read")],
            authenticationType: "TestAuth");
        var context = CreateContext(requirement, new ClaimsPrincipal(identity));

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_User_Lacks_Permission_Claim()
    {
        var requirement = new PermissionRequirement("Users.Delete");
        var identity = new ClaimsIdentity(
            [new Claim(PermissionAuthorizationHandler.PermissionClaimType, "Users.Read")],
            authenticationType: "TestAuth");
        var context = CreateContext(requirement, new ClaimsPrincipal(identity));

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        context.HasFailed.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_User_Is_Not_Authenticated()
    {
        var requirement = new PermissionRequirement("Users.Read");
        // No authenticationType => Identity.IsAuthenticated is false, even with a matching claim.
        var identity = new ClaimsIdentity(
            [new Claim(PermissionAuthorizationHandler.PermissionClaimType, "Users.Read")]);
        var context = CreateContext(requirement, new ClaimsPrincipal(identity));

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        context.HasFailed.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Match_Permission_Claim_Case_Insensitively()
    {
        var requirement = new PermissionRequirement("Users.Read");
        var identity = new ClaimsIdentity(
            [new Claim(PermissionAuthorizationHandler.PermissionClaimType, "users.read")],
            authenticationType: "TestAuth");
        var context = CreateContext(requirement, new ClaimsPrincipal(identity));

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }
}

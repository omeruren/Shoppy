using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Security.Claims;
using System.Security.Principal;
using Shoppy.DataAccess.Context;

namespace Shoppy.Tests.Helpers;

/// <summary>
/// Factory that creates a fresh ApplicationDbContext backed by EF Core InMemory provider.
/// Each call produces an isolated database, preventing test cross-contamination.
/// </summary>
public static class DbContextFactory
{
    public static ApplicationDbContext Create(string? databaseName = null)
    {
        var dbName = databaseName ?? Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        // The production code does: _httpContextAccessor?.HttpContext.User?.Identity?.IsAuthenticated
        // Note: there is NO null-guard on .HttpContext, so returning null would cause NRE.
        // We must mock the entire chain with IsAuthenticated = false to safely bypass the auth block.
        var identity = Substitute.For<IIdentity>();
        identity.IsAuthenticated.Returns(false);

        var claimsPrincipal = Substitute.For<ClaimsPrincipal>();
        claimsPrincipal.Identity.Returns(identity);

        var httpContext = Substitute.For<HttpContext>();
        httpContext.User.Returns(claimsPrincipal);

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var context = new ApplicationDbContext(options, httpContextAccessor);
        context.Database.EnsureCreated();
        return context;
    }
}

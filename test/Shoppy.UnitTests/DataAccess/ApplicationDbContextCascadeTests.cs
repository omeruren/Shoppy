using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;
using System.Security.Claims;

namespace Shoppy.UnitTests.DataAccess;

public class ApplicationDbContextCascadeTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _context;
    private readonly Guid _userId = Guid.NewGuid();

    public ApplicationDbContextCascadeTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, _userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        httpContext.User = new ClaimsPrincipal(identity);
        _httpContextAccessor.HttpContext.Returns(httpContext);

        _context = new ApplicationDbContext(_options, _httpContextAccessor);
    }

    // Creates a new ApplicationDbContext instance against the same InMemory database,
    // with an empty change tracker — needed to genuinely test "navigation not loaded"
    // scenarios, since reusing _context would resolve to the same tracked/loaded instance
    // via EF's identity map regardless of whether .Include() is used on the new query.
    private ApplicationDbContext CreateFreshContext() => new(_options, _httpContextAccessor);

    private async Task<Order> SeedOrderWithItemsAsync(int itemCount = 2)
    {
        var order = new Order { OrderDate = DateTimeOffset.UtcNow, Items = [] };
        for (var i = 0; i < itemCount; i++)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = Guid.NewGuid(),
                Quantity = i + 1,
                UnitPrice = 10m
            });
        }
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    [Fact]
    public async Task SaveChangesAsync_Should_SoftDelete_LoadedChildItems_When_Parent_Order_Is_Deleted()
    {
        // Arrange
        var seeded = await SeedOrderWithItemsAsync();
        var itemIds = seeded.Items.Select(i => i.Id).ToList();

        using var actContext = CreateFreshContext();
        var order = await actContext.Orders.Include(o => o.Items).FirstAsync(o => o.Id == seeded.Id);

        // Act
        actContext.Orders.Remove(order);
        await actContext.SaveChangesAsync();

        // Assert
        using var assertContext = CreateFreshContext();
        var children = await assertContext.OrderItems.IgnoreQueryFilters()
            .Where(i => itemIds.Contains(i.Id))
            .ToListAsync();

        children.Should().HaveCount(itemIds.Count);
        children.Should().OnlyContain(i => i.IsDeleted);
        children.Should().OnlyContain(i => i.DeletedAt != null);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Not_Cascade_When_Child_Collection_Not_Loaded()
    {
        // Arrange
        var seeded = await SeedOrderWithItemsAsync();
        var itemIds = seeded.Items.Select(i => i.Id).ToList();

        // Fresh context + no .Include(o => o.Items) — pins the precondition that cascade
        // only fires for navigations the caller actually loaded.
        using var actContext = CreateFreshContext();
        var order = await actContext.Orders.FirstAsync(o => o.Id == seeded.Id);

        // Act
        actContext.Orders.Remove(order);
        await actContext.SaveChangesAsync();

        // Assert
        using var assertContext = CreateFreshContext();
        var children = await assertContext.OrderItems.IgnoreQueryFilters()
            .Where(i => itemIds.Contains(i.Id))
            .ToListAsync();

        children.Should().OnlyContain(i => !i.IsDeleted);
    }
}

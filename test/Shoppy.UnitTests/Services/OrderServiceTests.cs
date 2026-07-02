using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Shoppy.Business.Extensions;
using Shoppy.Business.Orders;
using Shoppy.Business.Orders.DataTransferObjects;
using Shoppy.Business.OrderItems.DataTransferObjects;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;
using System.Security.Claims;

namespace Shoppy.UnitTests.Services;

public class OrderServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly OrderService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public OrderServiceTests()
    {
        // Isolated InMemory database per test class instance
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Stub HttpContextAccessor so audit fields (CreatedBy, UpdatedBy) are populated
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, _userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        httpContext.User = new ClaimsPrincipal(identity);
        httpContextAccessor.HttpContext.Returns(httpContext);

        _context = new ApplicationDbContext(options, httpContextAccessor);

        // Stub IMemoryCache — always return cache miss so service hits the database
        _cache = Substitute.For<IMemoryCache>();
        object? cacheEntry = null;
        _cache.TryGetValue(Arg.Any<object>(), out cacheEntry).Returns(false);

        _service = new OrderService(_context, _cache);
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    private Order BuildOrder(DateTimeOffset? orderDate = null)
    {
        return new Order
        {
            OrderDate = orderDate ?? DateTimeOffset.UtcNow,
            Items = []
        };
    }

    private async Task<Order> SeedOrderAsync(int itemCount = 0)
    {
        var order = new Order { OrderDate = DateTimeOffset.UtcNow, Items = [] };
        for (var i = 0; i < itemCount; i++)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = Guid.NewGuid(),
                Quantity = i + 1,
                UnitPrice = (i + 1) * 10m
            });
        }
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    // ─────────────────────────────────────────────
    //  GetByIdAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_Should_Return_Order_When_Exists()
    {
        // Arrange
        var order = await SeedOrderAsync(itemCount: 2);

        // Act
        var result = await _service.GetByIdAsync(order.Id, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(order.Id);
        result.Data.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Fail_When_Order_Does_Not_Exist()
    {
        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.ErrorMessages.Should().Contain("Order not found.");
    }

    // ─────────────────────────────────────────────
    //  GetAllAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_Should_Return_Paginated_Orders()
    {
        // Arrange
        await SeedOrderAsync();
        await SeedOrderAsync();
        await SeedOrderAsync();

        var request = new PaginationRequestDto(1, 10, string.Empty);

        // Act
        var result = await _service.GetAllAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Data!.TotalCount.Should().Be(3);
        result.Data.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_Empty_When_No_Orders_Exist()
    {
        // Arrange
        var request = new PaginationRequestDto(1, 10, string.Empty);

        // Act
        var result = await _service.GetAllAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Data!.TotalCount.Should().Be(0);
        result.Data.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_Should_Respect_PageSize()
    {
        // Arrange
        for (var i = 0; i < 5; i++)
            await SeedOrderAsync();

        var request = new PaginationRequestDto(1, 2, string.Empty);

        // Act
        var result = await _service.GetAllAsync(request, CancellationToken.None);

        // Assert
        result.Data!.TotalCount.Should().Be(5);
        result.Data.Data.Should().HaveCount(2);
        result.Data.TotalPageCount.Should().Be(3);
    }

    // ─────────────────────────────────────────────
    //  CreateAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_Should_Create_Order_And_Return_Success()
    {
        // Arrange
        var dto = new OrderCreateDto(
        [
            new OrderItemCreateDto(Guid.NewGuid(), 2),
            new OrderItemCreateDto(Guid.NewGuid(), 5)
        ]);

        // Act
        var result = await _service.CreateAsync(dto, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().Be("Order created.");
        _context.Orders.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateAsync_Should_Set_OrderDate_To_Current_Time()
    {
        // Arrange
        var dto = new OrderCreateDto([new OrderItemCreateDto(Guid.NewGuid(), 1)]);
        var before = DateTimeOffset.Now.AddSeconds(-1);

        // Act
        await _service.CreateAsync(dto, CancellationToken.None);
        var after = DateTimeOffset.Now.AddSeconds(1);

        // Assert
        var saved = _context.Orders.First();
        saved.OrderDate.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task CreateAsync_Should_Set_CreatedBy_To_Authenticated_UserId()
    {
        // Arrange
        var dto = new OrderCreateDto([new OrderItemCreateDto(Guid.NewGuid(), 1)]);

        // Act
        await _service.CreateAsync(dto, CancellationToken.None);

        // Assert
        var saved = _context.Orders.First();
        saved.CreatedBy.Should().Be(_userId);
    }

    // ─────────────────────────────────────────────
    //  UpdateAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_Should_Update_Order_When_Exists()
    {
        // Arrange
        var order = await SeedOrderAsync();
        var newDate = DateTimeOffset.UtcNow.AddDays(3);
        var dto = new OrderUpdateDto(order.Id, newDate, [], null);

        // Act
        var result = await _service.UpdateAsync(dto, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().Be("Order updated.");

        var updated = await _context.Orders.FindAsync(order.Id);
        updated!.OrderDate.Should().BeCloseTo(newDate, TimeSpan.FromSeconds(1));
        updated.UpdatedBy.Should().Be(_userId);
    }

    [Fact]
    public async Task UpdateAsync_Should_Fail_When_Order_Does_Not_Exist()
    {
        // Arrange
        var dto = new OrderUpdateDto(Guid.NewGuid(), DateTimeOffset.UtcNow, [], null);

        // Act
        var result = await _service.UpdateAsync(dto, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.ErrorMessages.Should().Contain("Order not found.");
    }

    // ─────────────────────────────────────────────
    //  DeleteAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_Should_SoftDelete_Order_When_Exists()
    {
        // Arrange
        var order = await SeedOrderAsync();

        // Act
        var result = await _service.DeleteAsync(order.Id, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().Be("Order deleted.");

        var deleted = await _context.Orders.IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == order.Id);
        deleted!.IsDeleted.Should().BeTrue();
        deleted.DeletedBy.Should().Be(_userId);
    }

    [Fact]
    public async Task DeleteAsync_Should_Fail_When_Order_Does_Not_Exist()
    {
        // Act
        var result = await _service.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.ErrorMessages.Should().Contain("Order not found.");
    }
}

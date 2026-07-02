using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Shoppy.Business.Extensions;
using Shoppy.Business.OrderItems;
using Shoppy.Business.OrderItems.DataTransferObjects;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;
using System.Security.Claims;

namespace Shoppy.UnitTests.Services;

public class OrderItemServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly OrderItemService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public OrderItemServiceTests()
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

        _service = new OrderItemService(_context, _cache);
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    private async Task<(Order order, OrderItem item)> SeedOneItemAsync(
        int quantity = 3, decimal unitPrice = 15.99m)
    {
        var order = new Order { OrderDate = DateTimeOffset.UtcNow, Items = [] };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var item = new OrderItem
        {
            OrderId = order.Id,
            ProductId = Guid.NewGuid(),
            Quantity = quantity,
            UnitPrice = unitPrice
        };
        _context.OrderItems.Add(item);
        await _context.SaveChangesAsync();

        return (order, item);
    }

    // ─────────────────────────────────────────────
    //  GetByIdAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_Should_Return_Item_When_Exists()
    {
        // Arrange
        var (_, item) = await SeedOneItemAsync();

        // Act
        var result = await _service.GetByIdAsync(item.Id, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(item.Id);
        result.Data.ProductId.Should().Be(item.ProductId);
        result.Data.Quantity.Should().Be(item.Quantity);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Fail_When_Item_Does_Not_Exist()
    {
        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessages.Should().Contain("Order item not found.");
    }

    // ─────────────────────────────────────────────
    //  GetAllAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_Should_Return_Paginated_Items()
    {
        // Arrange
        var order = new Order { OrderDate = DateTimeOffset.UtcNow, Items = [] };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        for (var i = 0; i < 3; i++)
        {
            _context.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = Guid.NewGuid(),
                Quantity = i + 1,
                UnitPrice = 10m
            });
        }
        await _context.SaveChangesAsync();

        var request = new PaginationRequestDto(1, 10, string.Empty);

        // Act
        var result = await _service.GetAllAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Data!.TotalCount.Should().Be(3);
        result.Data.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_Empty_When_No_Items_Exist()
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
        var order = new Order { OrderDate = DateTimeOffset.UtcNow, Items = [] };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        for (var i = 0; i < 5; i++)
        {
            _context.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = Guid.NewGuid(),
                Quantity = 1,
                UnitPrice = 10m
            });
        }
        await _context.SaveChangesAsync();

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
    public async Task CreateAsync_Should_Create_Item_And_Return_Success()
    {
        // Arrange
        var dto = new OrderItemCreateDto(Guid.NewGuid(), 5);

        // Act
        var result = await _service.CreateAsync(dto, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().Be("Order item created.");
        _context.OrderItems.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateAsync_Should_Persist_Correct_Values()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var dto = new OrderItemCreateDto(productId, 7);

        // Act
        await _service.CreateAsync(dto, CancellationToken.None);

        // Assert
        var saved = _context.OrderItems.First();
        saved.ProductId.Should().Be(productId);
        saved.Quantity.Should().Be(7);
        saved.CreatedBy.Should().Be(_userId);
    }

    // ─────────────────────────────────────────────
    //  UpdateAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_Should_Update_Item_When_Exists()
    {
        // Arrange
        var (_, item) = await SeedOneItemAsync();
        var newProductId = Guid.NewGuid();
        var dto = new OrderItemUpdateDto(item.Id, newProductId, 99, null);

        // Act
        var result = await _service.UpdateAsync(dto, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().Be("Order item updated.");

        var updated = await _context.OrderItems.FindAsync(item.Id);
        updated!.ProductId.Should().Be(newProductId);
        updated.Quantity.Should().Be(99);
        updated.UpdatedBy.Should().Be(_userId);
    }

    [Fact]
    public async Task UpdateAsync_Should_Fail_When_Item_Does_Not_Exist()
    {
        // Arrange
        var dto = new OrderItemUpdateDto(Guid.NewGuid(), Guid.NewGuid(), 1, null);

        // Act
        var result = await _service.UpdateAsync(dto, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.ErrorMessages.Should().Contain("Order item not found.");
    }

    // ─────────────────────────────────────────────
    //  DeleteAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_Should_SoftDelete_Item_When_Exists()
    {
        // Arrange
        var (_, item) = await SeedOneItemAsync();

        // Act
        var result = await _service.DeleteAsync(item.Id, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().Be("Order item deleted.");

        // IgnoreQueryFilters to see soft-deleted records
        var deleted = await _context.OrderItems.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == item.Id);
        deleted!.IsDeleted.Should().BeTrue();
        deleted.DeletedBy.Should().Be(_userId);
    }

    [Fact]
    public async Task DeleteAsync_Should_Fail_When_Item_Does_Not_Exist()
    {
        // Act
        var result = await _service.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.ErrorMessages.Should().Contain("Order item not found.");
    }
}

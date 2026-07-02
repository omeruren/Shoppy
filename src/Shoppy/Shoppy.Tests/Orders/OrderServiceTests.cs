using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Shoppy.Business.Orders;
using Shoppy.Business.Orders.DataTransferObjects;
using Shoppy.Business.OrderItems.DataTransferObjects;
using Shoppy.Business.Extensions;
using Shoppy.Entity.Models;
using Shoppy.Tests.Helpers;

namespace Shoppy.Tests.Orders;

public sealed class OrderServiceTests : IDisposable
{
    private readonly IMemoryCache _cache;

    public OrderServiceTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    public void Dispose() => _cache.Dispose();

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    private OrderService CreateSut(out Shoppy.DataAccess.Context.ApplicationDbContext context)
    {
        context = DbContextFactory.Create();
        return new OrderService(context, _cache);
    }

    private static Order BuildOrder(DateTimeOffset? orderDate = null)
    {
        var order = new Order
        {
            OrderDate = orderDate ?? DateTimeOffset.UtcNow,
            Items = []
        };
        return order;
    }

    private static Order BuildOrderWithItems(int itemCount = 2)
    {
        var order = BuildOrder();
        for (var i = 0; i < itemCount; i++)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = Guid.NewGuid(),
                Quantity = i + 1,
                UnitPrice = (i + 1) * 10m
            });
        }
        return order;
    }

    // ─────────────────────────────────────────────
    //  GetByIdAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsSuccessWithOrder()
    {
        // Arrange
        var sut = CreateSut(out var ctx);
        var order = BuildOrderWithItems();
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        // Act
        var result = await sut.GetByIdAsync(order.Id, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Data);
        Assert.Equal(order.Id, result.Data.Id);
        Assert.Equal(order.Items.Count, result.Data.Items.Count);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsFailureWith404()
    {
        // Arrange
        var sut = CreateSut(out _);

        // Act
        var result = await sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal(404, result.StatusCode);
        Assert.Contains("Order not found.", result.ErrorMessages);
    }

    // ─────────────────────────────────────────────
    //  GetAllAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_WithOrders_ReturnsPaginatedList()
    {
        // Arrange
        var sut = CreateSut(out var ctx);
        ctx.Orders.AddRange(BuildOrderWithItems(), BuildOrderWithItems(), BuildOrderWithItems());
        await ctx.SaveChangesAsync();

        var request = new PaginationRequestDto(1, 10, string.Empty);

        // Act
        var result = await sut.GetAllAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.TotalCount);
        Assert.Equal(3, result.Data.Data.Count);
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyPage()
    {
        // Arrange
        var sut = CreateSut(out _);
        var request = new PaginationRequestDto(1, 10, string.Empty);

        // Act
        var result = await sut.GetAllAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Data);
        Assert.Equal(0, result.Data.TotalCount);
        Assert.Empty(result.Data.Data);
    }

    [Fact]
    public async Task GetAllAsync_SecondCall_ReturnsCachedResult()
    {
        // Arrange
        var sut = CreateSut(out var ctx);
        ctx.Orders.Add(BuildOrderWithItems());
        await ctx.SaveChangesAsync();

        var request = new PaginationRequestDto(1, 10, string.Empty);

        // First call — populates cache
        var first = await sut.GetAllAsync(request, CancellationToken.None);

        // Add another order after cache is set
        ctx.Orders.Add(BuildOrderWithItems());
        await ctx.SaveChangesAsync();

        // Act — second call should still return cached (1 order)
        var second = await sut.GetAllAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(first.Data!.TotalCount, second.Data!.TotalCount);
    }

    [Fact]
    public async Task GetAllAsync_PageSize_RespectsPageSize()
    {
        // Arrange
        var sut = CreateSut(out var ctx);
        for (var i = 0; i < 5; i++)
            ctx.Orders.Add(BuildOrderWithItems());
        await ctx.SaveChangesAsync();

        var request = new PaginationRequestDto(1, 2, string.Empty);

        // Act
        var result = await sut.GetAllAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(5, result.Data!.TotalCount);
        Assert.Equal(2, result.Data.Data.Count);
        Assert.Equal(3, result.Data.TotalPageCount);
    }

    // ─────────────────────────────────────────────
    //  CreateAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsSuccessMessage()
    {
        // Arrange
        var sut = CreateSut(out var ctx);
        var dto = new OrderCreateDto(
        [
            new OrderItemCreateDto(Guid.NewGuid(), 2),
            new OrderItemCreateDto(Guid.NewGuid(), 5)
        ]);

        // Act
        var result = await sut.CreateAsync(dto, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal("Order created.", result.Data);
        Assert.Equal(1, ctx.Orders.Count());
    }

    [Fact]
    public async Task CreateAsync_SetsOrderDate_ToCurrentTime()
    {
        // Arrange
        var sut = CreateSut(out var ctx);
        var before = DateTimeOffset.Now.AddSeconds(-1);
        var dto = new OrderCreateDto([new OrderItemCreateDto(Guid.NewGuid(), 1)]);

        // Act
        await sut.CreateAsync(dto, CancellationToken.None);
        var after = DateTimeOffset.Now.AddSeconds(1);

        // Assert
        var savedOrder = ctx.Orders.First();
        Assert.InRange(savedOrder.OrderDate, before, after);
    }

    [Fact]
    public async Task CreateAsync_InvalidatesCache()
    {
        // Arrange
        var sut = CreateSut(out var ctx);
        var request = new PaginationRequestDto(1, 10, string.Empty);

        // Populate cache with 0 orders
        await sut.GetAllAsync(request, CancellationToken.None);

        // Add an order via CreateAsync (should bust cache)
        var dto = new OrderCreateDto([new OrderItemCreateDto(Guid.NewGuid(), 1)]);
        await sut.CreateAsync(dto, CancellationToken.None);

        // Act
        var result = await sut.GetAllAsync(request, CancellationToken.None);

        // Assert — cache was invalidated so new order is visible
        Assert.Equal(1, result.Data!.TotalCount);
    }

    // ─────────────────────────────────────────────
    //  UpdateAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingOrder_ReturnsSuccessMessage()
    {
        // Arrange
        var sut = CreateSut(out var ctx);
        var order = BuildOrder(DateTimeOffset.UtcNow.AddDays(-1));
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        var newDate = DateTimeOffset.UtcNow;
        var dto = new OrderUpdateDto(order.Id, newDate, [], null);

        // Act
        var result = await sut.UpdateAsync(dto, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal("Order updated.", result.Data);

        // Reload from DB
        var updated = ctx.Orders.First(o => o.Id == order.Id);
        Assert.Equal(newDate, updated.OrderDate);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ReturnsFailureWith404()
    {
        // Arrange
        var sut = CreateSut(out _);
        var dto = new OrderUpdateDto(Guid.NewGuid(), DateTimeOffset.UtcNow, [], null);

        // Act
        var result = await sut.UpdateAsync(dto, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal(404, result.StatusCode);
        Assert.Contains("Order not found.", result.ErrorMessages);
    }

    [Fact]
    public async Task UpdateAsync_InvalidatesCache()
    {
        // Arrange
        var sut = CreateSut(out var ctx);
        var order = BuildOrder();
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        var paginationRequest = new PaginationRequestDto(1, 10, string.Empty);
        // Populate cache
        var cached = await sut.GetAllAsync(paginationRequest, CancellationToken.None);
        var originalDate = cached.Data!.Data.First().OrderDate;

        var newDate = DateTimeOffset.UtcNow.AddDays(5);
        var updateDto = new OrderUpdateDto(order.Id, newDate, [], null);
        await sut.UpdateAsync(updateDto, CancellationToken.None);

        // Act
        var fresh = await sut.GetAllAsync(paginationRequest, CancellationToken.None);

        // Assert
        Assert.NotEqual(originalDate, fresh.Data!.Data.First().OrderDate);
    }

    // ─────────────────────────────────────────────
    //  DeleteAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsSuccessMessage()
    {
        // Arrange
        var sut = CreateSut(out var ctx);
        var order = BuildOrder();
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        // Act
        var result = await sut.DeleteAsync(order.Id, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal("Order deleted.", result.Data);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_SoftDeletesOrder()
    {
        // Arrange
        var sut = CreateSut(out var ctx);
        var order = BuildOrder();
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        // Act
        await sut.DeleteAsync(order.Id, CancellationToken.None);

        // Assert — DbContext SaveChanges intercept converts Remove → soft-delete
        var deletedOrder = ctx.Orders.IgnoreQueryFilters().FirstOrDefault(o => o.Id == order.Id);
        Assert.NotNull(deletedOrder);
        Assert.True(deletedOrder.IsDeleted);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsFailureWith404()
    {
        // Arrange
        var sut = CreateSut(out _);

        // Act
        var result = await sut.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal(404, result.StatusCode);
        Assert.Contains("Order not found.", result.ErrorMessages);
    }

    [Fact]
    public async Task DeleteAsync_InvalidatesCache()
    {
        // Arrange
        var sut = CreateSut(out var ctx);
        var order = BuildOrder();
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        var paginationRequest = new PaginationRequestDto(1, 10, string.Empty);
        // Warm up cache
        await sut.GetAllAsync(paginationRequest, CancellationToken.None);

        // Act
        await sut.DeleteAsync(order.Id, CancellationToken.None);
        var afterDelete = await sut.GetAllAsync(paginationRequest, CancellationToken.None);

        // Assert — cache was invalidated; soft-deleted order no longer appears
        Assert.Equal(0, afterDelete.Data!.TotalCount);
    }
}

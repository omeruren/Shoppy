using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Shoppy.Business.OrderItems;
using Shoppy.Business.OrderItems.DataTransferObjects;
using Shoppy.Business.Extensions;
using Shoppy.Entity.Models;
using Shoppy.Tests.Helpers;

namespace Shoppy.Tests.OrderItems;

public sealed class OrderItemServiceTests : IDisposable
{
    private readonly IMemoryCache _cache;

    public OrderItemServiceTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    public void Dispose() => _cache.Dispose();

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    private OrderItemService CreateSut(out Shoppy.DataAccess.Context.ApplicationDbContext context)
    {
        context = DbContextFactory.Create();
        return new OrderItemService(context, _cache);
    }

    private static Order BuildOrder()
    {
        var order = new Order
        {
            OrderDate = DateTimeOffset.UtcNow,
            Items = []
        };
        return order;
    }

    private static OrderItem BuildOrderItem(Guid orderId)
    {
        return new OrderItem
        {
            OrderId = orderId,
            ProductId = Guid.NewGuid(),
            Quantity = 3,
            UnitPrice = 15.99m
        };
    }

    private async Task<(Order order, OrderItem item)> SeedOneItemAsync(
        Shoppy.DataAccess.Context.ApplicationDbContext ctx)
    {
        var order = BuildOrder();
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        var item = BuildOrderItem(order.Id);
        ctx.OrderItems.Add(item);
        await ctx.SaveChangesAsync();

        return (order, item);
    }

    // ─────────────────────────────────────────────
    //  GetByIdAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsSuccessWithItem()
    {
        // Arrange
        var sut = CreateSut(out var ctx);
        var (_, item) = await SeedOneItemAsync(ctx);

        // Act
        var result = await sut.GetByIdAsync(item.Id, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Data);
        Assert.Equal(item.Id, result.Data.Id);
        Assert.Equal(item.ProductId, result.Data.ProductId);
        Assert.Equal(item.Quantity, result.Data.Quantity);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsFailure()
    {
        // Arrange
        var sut = CreateSut(out _);

        // Act
        var result = await sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Contains("Order item not found.", result.ErrorMessages);
    }

    // ─────────────────────────────────────────────
    //  GetAllAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_WithItems_ReturnsPaginatedList()
    {
        // Arrange
        var sut = CreateSut(out var ctx);
        var order = BuildOrder();
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        ctx.OrderItems.AddRange(
            BuildOrderItem(order.Id),
            BuildOrderItem(order.Id),
            BuildOrderItem(order.Id));
        await ctx.SaveChangesAsync();

        var request = new PaginationRequestDto(1, 10, string.Empty);

        // Act
        var result = await sut.GetAllAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(3, result.Data!.TotalCount);
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
        Assert.Equal(0, result.Data!.TotalCount);
        Assert.Empty(result.Data.Data);
    }

    [Fact]
    public async Task GetAllAsync_SecondCall_ReturnsCachedResult()
    {
        // Arrange
        var sut = CreateSut(out var ctx);
        var (_, _) = await SeedOneItemAsync(ctx);

        var request = new PaginationRequestDto(1, 10, string.Empty);

        // Warm cache
        var first = await sut.GetAllAsync(request, CancellationToken.None);

        // Add item directly — bypasses cache invalidation
        var order2 = BuildOrder();
        ctx.Orders.Add(order2);
        await ctx.SaveChangesAsync();
        ctx.OrderItems.Add(BuildOrderItem(order2.Id));
        await ctx.SaveChangesAsync();

        // Act
        var second = await sut.GetAllAsync(request, CancellationToken.None);

        // Assert — still returns cached value (1 item)
        Assert.Equal(first.Data!.TotalCount, second.Data!.TotalCount);
    }

    [Fact]
    public async Task GetAllAsync_PageSize_RespectsPageSize()
    {
        // Arrange
        var sut = CreateSut(out var ctx);
        var order = BuildOrder();
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        for (var i = 0; i < 5; i++)
            ctx.OrderItems.Add(BuildOrderItem(order.Id));
        await ctx.SaveChangesAsync();

        var request = new PaginationRequestDto(1, 2, string.Empty);

        // Act
        var result = await sut.GetAllAsync(request, CancellationToken.None);

        // Assert
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
        var dto = new OrderItemCreateDto(Guid.NewGuid(), 5);

        // Act
        var result = await sut.CreateAsync(dto, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal("Order item created.", result.Data);
        Assert.Equal(1, ctx.OrderItems.Count());
    }

    [Fact]
    public async Task CreateAsync_PersistsCorrectValues()
    {
        // Arrange
        var sut = CreateSut(out var ctx);
        var productId = Guid.NewGuid();
        var dto = new OrderItemCreateDto(productId, 7);

        // Act
        await sut.CreateAsync(dto, CancellationToken.None);

        // Assert
        var saved = ctx.OrderItems.First();
        Assert.Equal(productId, saved.ProductId);
        Assert.Equal(7, saved.Quantity);
    }

    [Fact]
    public async Task CreateAsync_InvalidatesCache()
    {
        // Arrange
        var sut = CreateSut(out _);
        var request = new PaginationRequestDto(1, 10, string.Empty);

        // Warm cache (0 items)
        await sut.GetAllAsync(request, CancellationToken.None);

        // Act
        var dto = new OrderItemCreateDto(Guid.NewGuid(), 1);
        await sut.CreateAsync(dto, CancellationToken.None);
        var result = await sut.GetAllAsync(request, CancellationToken.None);

        // Assert — cache was busted
        Assert.Equal(1, result.Data!.TotalCount);
    }

    // ─────────────────────────────────────────────
    //  UpdateAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingItem_ReturnsSuccessMessage()
    {
        // Arrange
        var sut = CreateSut(out var ctx);
        var (_, item) = await SeedOneItemAsync(ctx);

        var dto = new OrderItemUpdateDto(item.Id, Guid.NewGuid(), 10, null);

        // Act
        var result = await sut.UpdateAsync(dto, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal("Order item updated.", result.Data);
    }

    [Fact]
    public async Task UpdateAsync_ExistingItem_PersistsNewValues()
    {
        // Arrange
        var sut = CreateSut(out var ctx);
        var (_, item) = await SeedOneItemAsync(ctx);

        var newProductId = Guid.NewGuid();
        var dto = new OrderItemUpdateDto(item.Id, newProductId, 99, null);

        // Act
        await sut.UpdateAsync(dto, CancellationToken.None);

        // Assert
        var updated = ctx.OrderItems.First(x => x.Id == item.Id);
        Assert.Equal(newProductId, updated.ProductId);
        Assert.Equal(99, updated.Quantity);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ReturnsFailureWith404()
    {
        // Arrange
        var sut = CreateSut(out _);
        var dto = new OrderItemUpdateDto(Guid.NewGuid(), Guid.NewGuid(), 1, null);

        // Act
        var result = await sut.UpdateAsync(dto, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal(404, result.StatusCode);
        Assert.Contains("Order item not found.", result.ErrorMessages);
    }

    [Fact]
    public async Task UpdateAsync_InvalidatesCache()
    {
        // Arrange
        var sut = CreateSut(out var ctx);
        var (_, item) = await SeedOneItemAsync(ctx);
        var request = new PaginationRequestDto(1, 10, string.Empty);

        // Warm cache
        var cached = await sut.GetAllAsync(request, CancellationToken.None);
        var originalQty = cached.Data!.Data.First().Quantity;

        var dto = new OrderItemUpdateDto(item.Id, item.ProductId, originalQty + 100, null);
        await sut.UpdateAsync(dto, CancellationToken.None);

        // Act
        var fresh = await sut.GetAllAsync(request, CancellationToken.None);

        // Assert
        Assert.NotEqual(originalQty, fresh.Data!.Data.First().Quantity);
    }

    // ─────────────────────────────────────────────
    //  DeleteAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsSuccessMessage()
    {
        // Arrange
        var sut = CreateSut(out var ctx);
        var (_, item) = await SeedOneItemAsync(ctx);

        // Act
        var result = await sut.DeleteAsync(item.Id, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal("Order item deleted.", result.Data);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_SoftDeletesItem()
    {
        // Arrange
        var sut = CreateSut(out var ctx);
        var (_, item) = await SeedOneItemAsync(ctx);

        // Act
        await sut.DeleteAsync(item.Id, CancellationToken.None);

        // Assert — soft-delete: record still exists but IsDeleted = true
        var deletedItem = ctx.OrderItems.IgnoreQueryFilters().FirstOrDefault(x => x.Id == item.Id);
        Assert.NotNull(deletedItem);
        Assert.True(deletedItem.IsDeleted);
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
        Assert.Contains("Order item not found.", result.ErrorMessages);
    }

    [Fact]
    public async Task DeleteAsync_InvalidatesCache()
    {
        // Arrange
        var sut = CreateSut(out var ctx);
        var (_, item) = await SeedOneItemAsync(ctx);
        var request = new PaginationRequestDto(1, 10, string.Empty);

        // Warm cache
        await sut.GetAllAsync(request, CancellationToken.None);

        // Act
        await sut.DeleteAsync(item.Id, CancellationToken.None);
        var afterDelete = await sut.GetAllAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(0, afterDelete.Data!.TotalCount);
    }
}

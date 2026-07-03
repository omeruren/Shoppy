using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Caching;
using Shoppy.Business.Extensions;
using Shoppy.Business.OrderItems.DataTransferObjects;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;

namespace Shoppy.Business.OrderItems;

public sealed class OrderItemService(ApplicationDbContext _context, ICacheService _cacheService, ILogger<OrderItemService> _logger) : IOrderItemService
{
    private const string CacheKeyPrefix = "orderItems";

    private readonly DbSet<OrderItem> _orderItems = _context.Set<OrderItem>();

    // GET ALL ITEMS

    public async Task<Result<PaginationResultDto<OrderItemResultDto>>> GetAllAsync(PaginationRequestDto request, CancellationToken cancellationToken)
    {
        return await _cacheService.GetOrCreateAsync(CacheKeyPrefix, request.ToCacheKey(CacheKeyPrefix), async () =>
        {
            return await _orderItems
                .AsNoTracking()
                .Include(i => i.Product)
                .Where(oi => string.IsNullOrWhiteSpace(request.SearchTerm) || oi.Product.Name.Contains(request.SearchTerm))
                .Select(i => new OrderItemResultDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,

                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt,

                    IsDeleted = i.IsDeleted,
                    DeletedAt = i.DeletedAt

                })
                .ApplyOrderItemSort(request.SortBy, request.SortDirection)
                .WithPagination(request, cancellationToken);
        }, TimeSpan.FromMinutes(5));
    }

    // GET ITEM BY ID
    public async Task<Result<OrderItemResultDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var orderItem = await _orderItems.AsNoTracking().Include(i => i.Product).Where(i => i.Id == id).Select(i => new OrderItemResultDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            Quantity = i.Quantity,

            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt,

            IsDeleted = i.IsDeleted,
            DeletedAt = i.DeletedAt

        }).FirstOrDefaultAsync(cancellationToken);

        if (orderItem is null)
            return Result<OrderItemResultDto>.Failure(404, ErrorMessages.OrderItem.NotFound);

        return orderItem;
    }

    // CREATE ITEM

    public async Task<Result<string>> CreateAsync(OrderItemCreateDto request, CancellationToken cancellationToken)
    {
        var orderItem = request.Adapt<OrderItem>();

        _orderItems.Add(orderItem);

        await _context.SaveChangesAsync(cancellationToken);

        await _cacheService.InvalidatePrefixAsync(CacheKeyPrefix);

        _logger.LogInformation("OrderItem {OrderItemId} created", orderItem.Id);

        return Result<string>.Success("Order item created.", 201);
    }


    // UPDATE ITEM

    public async Task<Result<string>> UpdateAsync(OrderItemUpdateDto request, CancellationToken cancellationToken)
    {
        var orderItem = await _orderItems.FindAsync([request.Id], cancellationToken);

        if (orderItem is null)
            return Result<string>.Failure(404, ErrorMessages.OrderItem.NotFound);

        request.Adapt(orderItem);

        if (request.RowVersion is not null)
            _context.Entry(orderItem).Property(x => x.RowVersion).OriginalValue = request.RowVersion;

        _orderItems.Update(orderItem);
        await _context.SaveChangesAsync(cancellationToken);

        await _cacheService.InvalidatePrefixAsync(CacheKeyPrefix);

        _logger.LogInformation("OrderItem {OrderItemId} updated", orderItem.Id);

        return "Order item updated.";
    }

    // DELETE ITEM
    public async Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var orderItem = await _orderItems.FindAsync([id], cancellationToken);

        if (orderItem is null)
            return Result<string>.Failure(404, ErrorMessages.OrderItem.NotFound);

        _orderItems.Remove(orderItem);
        await _context.SaveChangesAsync(cancellationToken);

        await _cacheService.InvalidatePrefixAsync(CacheKeyPrefix);

        _logger.LogInformation("OrderItem {OrderItemId} deleted", orderItem.Id);

        return "Order item deleted.";
    }
}

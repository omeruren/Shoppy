using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Extensions;
using Shoppy.Business.OrderItems.DataTransferObjects;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;

namespace Shoppy.Business.OrderItems;

public class OrderItemService(ApplicationDbContext _context, IMemoryCache _cache) : IOrderItemService
{
    private const string CacheKeyPrefix = "orderItems";

    private readonly DbSet<OrderItem> _orderItems = _context.Set<OrderItem>();

    // Cache invalidation via CancellationTokenSource
    private static CancellationTokenSource _cacheResetToken = new();

    private static void InvalidateCache()
    {
        var oldToken = Interlocked.Exchange(ref _cacheResetToken, new CancellationTokenSource());
        oldToken.Cancel();
        oldToken.Dispose();
    }

    private static string BuildCacheKey(PaginationRequestDto request)
        => $"{CacheKeyPrefix}:p{request.PageNumber}:s{request.PageSize}:q{request.SearchTerm}";


    // GET ALL ITEMS

    public async Task<Result<PaginationResultDto<OrderItemResultDto>>> GetAllAsync(PaginationRequestDto request, CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey(request);

        var orderItems = _cache.Get<PaginationResultDto<OrderItemResultDto>>(cacheKey);

        if (orderItems is null)
        {

            orderItems = await _orderItems.AsNoTracking().Include(i => i.Product).Select(i => new OrderItemResultDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                Quantity = i.Quantity,

                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt,

                IsDeleted = i.IsDeleted,
                DeletedAt = i.DeletedAt

            })
               .WithPagination(request, cancellationToken);

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                .AddExpirationToken(new CancellationChangeToken(_cacheResetToken.Token));

            _cache.Set(cacheKey, orderItems, cacheOptions);
        }
        return orderItems;
    }

    // GET ITEM BY ID
    public async Task<Result<OrderItemResultDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var orderItems = await _orderItems.AsNoTracking().Include(i => i.Product).Select(i => new OrderItemResultDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            Quantity = i.Quantity,

            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt,

            IsDeleted = i.IsDeleted,
            DeletedAt = i.DeletedAt

        }).FirstOrDefaultAsync(cancellationToken);

        if (orderItems is null)
            return Result<OrderItemResultDto>.Failure("Order item not found.");

        return orderItems;
    }

    // CREATE ITEM

    public async Task<Result<string>> CreateAsync(OrderItemCreateDto request, CancellationToken cancellationToken)
    {
        var orderItem = request.Adapt<OrderItem>();

        _orderItems.Add(orderItem);

        await _context.SaveChangesAsync(cancellationToken);

        InvalidateCache();

        return "Order item created.";
    }


    // UPDATE ITEM

    public async Task<Result<string>> UpdateAsync(OrderItemUpdateDto request, CancellationToken cancellationToken)
    {
        var orderItem = await _orderItems.FindAsync([request.Id], cancellationToken);

        if (orderItem is null)
            return Result<string>.Failure(404, "Order item not found.");

        request.Adapt(orderItem);

        if (request.RowVersion is not null)
            _context.Entry(orderItem).Property(x => x.RowVersion).OriginalValue = request.RowVersion;

        _orderItems.Update(orderItem);
        await _context.SaveChangesAsync(cancellationToken);

        InvalidateCache();

        return "Order item updated.";
    }

    // DELETE ITEM
    public async Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var orderItem = await _orderItems.FindAsync([id], cancellationToken);

        if (orderItem is null)
            return Result<string>.Failure(404, "Order item not found.");

        _orderItems.Remove(orderItem);
        await _context.SaveChangesAsync(cancellationToken);

        InvalidateCache();

        return "Order item deleted.";
    }
}

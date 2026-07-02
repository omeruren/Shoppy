using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Extensions;
using Shoppy.Business.OrderItems.DataTransferObjects;
using Shoppy.Business.Orders.DataTransferObjects;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;

namespace Shoppy.Business.Orders;

public sealed class OrderService(ApplicationDbContext _context, IMemoryCache _cache) : IOrderService
{
    private const string CacheKeyPrefix = "orders";


    private readonly DbSet<Order> _orders = _context.Set<Order>();

    private static CancellationTokenSource _cacheResetToken = new();

    private static void InvalidateCache()
    {
        var oldToken = Interlocked.Exchange(ref _cacheResetToken, new CancellationTokenSource());
        oldToken.Cancel();
        oldToken.Dispose();
    }


    private static string BuildCacheKey(PaginationRequestDto request)
        => $"{CacheKeyPrefix}:p{request.PageNumber}:s{request.PageSize}:q{request.SearchTerm}";


    // GET ALL ORDERS

    public async Task<Result<PaginationResultDto<OrderResultDto>>> GetAllAsync(PaginationRequestDto request, CancellationToken cancellationToken)
    {
        string cacheKey = BuildCacheKey(request);

        var orders = _cache.Get<PaginationResultDto<OrderResultDto>>(cacheKey);

        if (orders is null)
        {

            orders = await _orders
               .AsNoTracking()
               .Include(o => o.Items)
               .Select(p => new OrderResultDto
               {
                   Id = p.Id,
                   OrderDate = p.OrderDate,
                   Items = p.Items.Select(i => new OrderItemResultDto
                   {
                       Id = i.Id,
                       ProductId = i.ProductId,
                       Quantity = i.Quantity,

                       CreatedAt = i.CreatedAt,
                       UpdatedAt = i.UpdatedAt,

                       IsDeleted = i.IsDeleted,
                       DeletedAt = i.DeletedAt
                   }).ToList(),

                   CreatedAt = p.CreatedAt,
                   CreatedBy = p.CreatedBy,

                   UpdatedAt = p.UpdatedAt,
                   UpdatedBy = p.UpdatedBy,

                   IsDeleted = p.IsDeleted,
                   DeletedAt = p.DeletedAt,
                   DeletedBy = p.DeletedBy

               })
               .WithPagination(request, cancellationToken);

            var cacheOptions = new MemoryCacheEntryOptions()
               .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
               .AddExpirationToken(new CancellationChangeToken(_cacheResetToken.Token));

            _cache.Set(cacheKey, orders, cacheOptions);
        }
        return orders;
    }


    // GET ORDER BY ID

    public async Task<Result<OrderResultDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Select(p => new OrderResultDto
            {
                Id = p.Id,
                OrderDate = p.OrderDate,
                Items = p.Items.Select(i => new OrderItemResultDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList()
            })
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order is null)
            return Result<OrderResultDto>.Failure(404, "Order not found.");

        return order;
    }

    // CREATE  ORDER

    public async Task<Result<string>> CreateAsync(OrderCreateDto request, CancellationToken cancellationToken)
    {
        var order = request.Adapt<Order>();

        order.OrderDate = DateTimeOffset.Now;

        _orders.Add(order);

        await _context.SaveChangesAsync(cancellationToken);

        InvalidateCache();

        return "Order created.";
    }

    // UPDATE ORDER

    public async Task<Result<string>> UpdateAsync(OrderUpdateDto request, CancellationToken cancellationToken)
    {
        Order? order = await _orders.FindAsync([request.Id], cancellationToken);

        if (order is null)
            return Result<string>.Failure(404, "Order not found.");

        request.Adapt(order);

        if (request.RowVersion is not null)
            _context.Entry(order).Property(x => x.RowVersion).OriginalValue = request.RowVersion;


        _orders.Update(order);
        await _context.SaveChangesAsync(cancellationToken);

        InvalidateCache();

        return "Order updated.";
    }


    // DELETE ORDER

    public async Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orders.FindAsync([id], cancellationToken);

        if (order is null)
            return Result<string>.Failure(404, "Order not found.");

        _orders.Remove(order);
        await _context.SaveChangesAsync(cancellationToken);

        InvalidateCache();

        return "Order deleted.";
    }




}
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Caching;
using Shoppy.Business.Extensions;
using Shoppy.Business.OrderItems.DataTransferObjects;
using Shoppy.Business.Orders.DataTransferObjects;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;
using System.Security.Claims;

namespace Shoppy.Business.Orders;

public sealed class OrderService(
    ApplicationDbContext _context,
    ICacheService _cacheService,
    ILogger<OrderService> _logger,
    IHttpContextAccessor _httpContextAccessor) : IOrderService
{
    private const string CacheKeyPrefix = "orders";

    private readonly DbSet<Order> _orders = _context.Set<Order>();
    private readonly DbSet<OrderItem> _orderItems = _context.Set<OrderItem>();

    // Orders are only ever visible to their own creator, except for Admins who manage all orders.
    // Resolved per-call (not cached on the instance) since scoped services are reused within a request only.
    private (Guid? UserId, bool IsAdmin) ResolveCaller()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
            return (null, false);

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userId = Guid.TryParse(userIdClaim, out var parsedId) ? parsedId : (Guid?)null;

        return (userId, user.IsInRole("Admin"));
    }

    // GET ALL ORDERS

    public async Task<Result<PaginationResultDto<OrderResultDto>>> GetAllAsync(PaginationRequestDto request, CancellationToken cancellationToken)
    {
        var (userId, isAdmin) = ResolveCaller();

        // Cache key must vary per caller — otherwise one customer's cached page could be served to another.
        var cacheKey = $"{request.ToCacheKey(CacheKeyPrefix)}:u{(isAdmin ? "admin" : userId?.ToString() ?? "anon")}";

        return await _cacheService.GetOrCreateAsync(CacheKeyPrefix, cacheKey, async () =>
        {
            return await _orders
               .AsNoTracking()
               .Where(o => isAdmin || o.CreatedBy == userId)
               .Where(o => string.IsNullOrWhiteSpace(request.SearchTerm)
                   || o.Items.Any(i => i.Product.Name.Contains(request.SearchTerm)))
               .Include(o => o.Items)
               .Select(p => new OrderResultDto
               {
                   Id = p.Id,
                   OrderDate = p.OrderDate,
                   RowVersion = p.RowVersion,
                   Items = p.Items.Select(i => new OrderItemResultDto
                   {
                       Id = i.Id,
                       ProductId = i.ProductId,
                       Quantity = i.Quantity,
                       RowVersion = i.RowVersion,

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
               .ApplyOrderSort(request.SortBy, request.SortDirection)
               .WithPagination(request, cancellationToken);
        }, TimeSpan.FromMinutes(5));
    }


    // GET ORDER BY ID

    public async Task<Result<OrderResultDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var (userId, isAdmin) = ResolveCaller();

        var order = await _orders
            .AsNoTracking()
            .Where(o => isAdmin || o.CreatedBy == userId)
            .Include(o => o.Items)
            .Select(p => new OrderResultDto
            {
                Id = p.Id,
                OrderDate = p.OrderDate,
                RowVersion = p.RowVersion,
                Items = p.Items.Select(i => new OrderItemResultDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    RowVersion = i.RowVersion
                }).ToList()
            })
            // A non-admin's order belonging to someone else is treated the same as
            // non-existent (404, not 403) — avoids leaking whether the order id exists at all.
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order is null)
            return Result<OrderResultDto>.Failure(404, ErrorMessages.Order.NotFound);

        return order;
    }

    // CREATE  ORDER

    public async Task<Result<string>> CreateAsync(OrderCreateDto request, CancellationToken cancellationToken)
    {
        var order = request.Adapt<Order>();

        order.OrderDate = DateTimeOffset.Now;

        _orders.Add(order);

        await _context.SaveChangesAsync(cancellationToken);

        await _cacheService.InvalidatePrefixAsync(CacheKeyPrefix);

        _logger.LogInformation("Order {OrderId} created", order.Id);

        return Result<string>.Success("Order created.", 201);
    }

    // UPDATE ORDER

    public async Task<Result<string>> UpdateAsync(OrderUpdateDto request, CancellationToken cancellationToken)
    {
        Order? order = await _orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (order is null)
            return Result<string>.Failure(404, ErrorMessages.Order.NotFound);

        order.OrderDate = request.OrderDate;

        if (request.RowVersion is not null)
            _context.Entry(order).Property(x => x.RowVersion).OriginalValue = request.RowVersion;

        // Reconcile Items against the DTO instead of letting Mapster replace the whole navigation
        // (order.Items is now loaded/tracked, so Add/Remove here translates to real inserts/soft-deletes
        // rather than duplicating or orphaning rows).
        var dtoIds = request.Items.Select(i => i.Id).ToHashSet();

        foreach (var existingItem in order.Items.ToList())
        {
            if (!dtoIds.Contains(existingItem.Id))
                order.Items.Remove(existingItem);
        }

        foreach (var itemDto in request.Items)
        {
            var existingItem = order.Items.FirstOrDefault(i => i.Id == itemDto.Id);

            if (existingItem is not null)
            {
                existingItem.ProductId = itemDto.ProductId;
                existingItem.Quantity = itemDto.Quantity;

                if (itemDto.RowVersion is not null)
                    _context.Entry(existingItem).Property(x => x.RowVersion).OriginalValue = itemDto.RowVersion;
            }
            else
            {
                // Explicitly Add() to the DbSet (not the navigation collection) — EF's Guid-key
                // convention is ValueGeneratedOnAdd, so a new entity merely appended to a tracked
                // navigation (with its non-default, client-generated Id already set) gets
                // ambiguously picked up by DetectChanges as Modified instead of Added. Adding it
                // to _orderItems directly avoids the ambiguity; EF's FK-based fixup then adds it
                // to order.Items automatically (adding it there too would duplicate the reference).
                _orderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _cacheService.InvalidatePrefixAsync(CacheKeyPrefix);

        _logger.LogInformation("Order {OrderId} updated", order.Id);

        return "Order updated.";
    }


    // DELETE ORDER

    public async Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order is null)
            return Result<string>.Failure(404, ErrorMessages.Order.NotFound);

        _orders.Remove(order);
        await _context.SaveChangesAsync(cancellationToken);

        await _cacheService.InvalidatePrefixAsync(CacheKeyPrefix);

        _logger.LogInformation("Order {OrderId} deleted", order.Id);

        return "Order deleted.";
    }
}

using Mapster;
using Microsoft.EntityFrameworkCore;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Extensions;
using Shoppy.Business.OrderItems.DataTransferObjects;
using Shoppy.Business.Orders.DataTransferObjects;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;

namespace Shoppy.Business.Orders;

public sealed class OrderService(ApplicationDbContext _context) : IOrderService
{
    private readonly DbSet<Order> _orders = _context.Set<Order>();

    // GET ALL ORDERS

    public async Task<Result<PaginationResultDto<OrderResultDto>>> GetAllAsync(PaginationRequestDto request, CancellationToken cancellationToken)
    {
        PaginationResultDto<OrderResultDto> orders = await _orders
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
                UpdatedAt = p.UpdatedAt,
                IsDeleted = p.IsDeleted,
                DeletedAt = p.DeletedAt

            })
            .WithPagination(request, cancellationToken);

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

        return "Order created.";
    }

    // UPDATE ORDER

    public async Task<Result<string>> UpdateAsync(OrderUpdateDto request, CancellationToken cancellationToken)
    {
        Order? order = await _orders.FindAsync([request.Id], cancellationToken);

        if (order is null)
            return Result<string>.Failure(404, "Order not found.");

        request.Adapt(order);

        _orders.Update(order);
        await _context.SaveChangesAsync(cancellationToken);

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

        return "Order deleted.";
    }




}
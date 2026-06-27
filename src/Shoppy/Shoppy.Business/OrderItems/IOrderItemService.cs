using Mapster;
using Microsoft.EntityFrameworkCore;
using Shoppy.Business.BaseResult;
using Shoppy.Business.OrderItems.DataTransferObjects;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;

namespace Shoppy.Business.OrderItems;

public interface IOrderItemService
{
    Task<Result<List<OrderItemResultDto>>> GetAllAsync(CancellationToken cancellationToken);
    Task<Result<OrderItemResultDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<string>> CreateAsync(OrderItemCreateDto request, CancellationToken cancellationToken);
    Task<Result<string>> UpdateAsync(OrderItemUpdateDto request, CancellationToken cancellationToken);
    Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
public class OrderItemService(ApplicationDbContext _context) : IOrderItemService
{
    private readonly DbSet<OrderItem> _orderItems = _context.Set<OrderItem>();

    // GET ALL ITEMS

    public async Task<Result<List<OrderItemResultDto>>> GetAllAsync(CancellationToken cancellationToken)
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

        }).ToListAsync(cancellationToken);

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

        return "Order item created.";
    }


    // UPDATE ITEM

    public async Task<Result<string>> UpdateAsync(OrderItemUpdateDto request, CancellationToken cancellationToken)
    {
        var orderItem = await _orderItems.FindAsync([request.Id], cancellationToken);

        if (orderItem is null)
            return Result<string>.Failure(404, "Order item not found.");

        _orderItems.Update(orderItem);
        await _context.SaveChangesAsync(cancellationToken);

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

        return "Order item deleted.";
    }
}

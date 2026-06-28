using Shoppy.Business.BaseResult;
using Shoppy.Business.OrderItems.DataTransferObjects;

namespace Shoppy.Business.OrderItems;

public interface IOrderItemService
{
    Task<Result<List<OrderItemResultDto>>> GetAllAsync(CancellationToken cancellationToken);
    Task<Result<OrderItemResultDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<string>> CreateAsync(OrderItemCreateDto request, CancellationToken cancellationToken);
    Task<Result<string>> UpdateAsync(OrderItemUpdateDto request, CancellationToken cancellationToken);
    Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

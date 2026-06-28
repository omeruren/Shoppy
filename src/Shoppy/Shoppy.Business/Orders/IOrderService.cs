using Shoppy.Business.BaseResult;
using Shoppy.Business.Orders.DataTransferObjects;

namespace Shoppy.Business.Orders;

public interface IOrderService
{
    Task<Result<List<OrderResultDto>>> GetAllAsync(CancellationToken cancellationToken);
    Task<Result<OrderResultDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<string>> CreateAsync(OrderCreateDto request, CancellationToken cancellationToken);
    Task<Result<string>> UpdateAsync(OrderUpdateDto request, CancellationToken cancellationToken);
    Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
using Shoppy.Business.BaseResult;
using Shoppy.Business.Extensions;
using Shoppy.Business.Products.DataTransferObjects;

namespace Shoppy.Business.Products;

public interface IProductService
{
    Task<Result<PaginationResultDto<ProductResultDto>>> GetAllAsync(PaginationRequestDto request, CancellationToken cancellationToken);
    Task<Result<ProductResultDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<string>> CreateAsync(ProductCreateDto request, CancellationToken cancellationToken);
    Task<Result<string>> UpdateAsync(ProductUpdateDto request, CancellationToken cancellationToken);
    Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

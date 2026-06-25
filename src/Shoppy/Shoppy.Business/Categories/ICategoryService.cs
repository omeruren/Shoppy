using Shoppy.Business.BaseResult;
using Shoppy.Business.Categories.DataTransferObjects;

namespace Shoppy.Business.Categories;

public interface ICategoryService
{
    Task<Result<List<CategoryResultDto>>> GetallAsync(CancellationToken cancellationToken);
    Task<Result<CategoryResultDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<string>> CreateAsync(CategoryCreateDto category, CancellationToken cancellationToken);
    Task<Result<string>> UpdateAsync(CategoryUpdateDto category, CancellationToken cancellationToken);
    Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

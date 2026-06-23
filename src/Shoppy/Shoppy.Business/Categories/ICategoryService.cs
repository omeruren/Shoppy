using Shoppy.Business.DataTransferObjects;

namespace Shoppy.Business.Categories;

public interface ICategoryService
{
    Task<List<CategoryResultDto>> GetallAsync(CancellationToken cancellationToken);
    Task<CategoryResultDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<string> CreateAsync(CategoryCreateDto category, CancellationToken cancellationToken);
    Task<string> UpdateAsync(CategoryUpdateDto category, CancellationToken cancellationToken);
    Task<string> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Caching;
using Shoppy.Business.Categories.DataTransferObjects;
using Shoppy.Business.Extensions;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;

namespace Shoppy.Business.Categories;

public sealed class CategoryService(ApplicationDbContext _context, ICacheService _cacheService, ILogger<CategoryService> _logger) : ICategoryService
{
    private const string CacheKeyPrefix = "categories";

    private readonly DbSet<Category> _categories = _context.Set<Category>();

    // Get All Categories
    public async Task<Result<PaginationResultDto<CategoryResultDto>>> GetallAsync(PaginationRequestDto request, CancellationToken cancellationToken)
    {
        return await _cacheService.GetOrCreateAsync(CacheKeyPrefix, request.ToCacheKey(CacheKeyPrefix), async () =>
        {
            return await _categories
                .AsNoTracking()
                .Where(c => string.IsNullOrWhiteSpace(request.SearchTerm) || c.Name.Contains(request.SearchTerm))
                .Select(p => new CategoryResultDto
                {
                    Id = p.Id,
                    Name = p.Name,

                    CreatedAt = p.CreatedAt,
                    CreatedBy = p.CreatedBy,

                    UpdatedAt = p.UpdatedAt,
                    UpdatedBy = p.UpdatedBy,

                    IsDeleted = p.IsDeleted,
                    DeletedAt = p.DeletedAt,
                    DeletedBy = p.DeletedBy

                })
                .ApplyCategorySort(request.SortBy, request.SortDirection)
                .WithPagination(request, cancellationToken);
        }, TimeSpan.FromMinutes(5));
    }

    // Get Category By Id

    public async Task<Result<CategoryResultDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        Category? category = await _categories.FindAsync(id, cancellationToken);

        if (category is null)
            return Result<CategoryResultDto>.Failure(404, ErrorMessages.Category.NotFound);

        var categoryResult = category.Adapt<CategoryResultDto>();

        return categoryResult;
    }

    // Create Category

    public async Task<Result<string>> CreateAsync(CategoryCreateDto request, CancellationToken cancellationToken)
    {
        bool isExists = await _categories.AnyAsync(c => c.Name.Equals(request.Name), cancellationToken);

        if (isExists)
            return Result<string>.Failure(409, ErrorMessages.Category.AlreadyExists);

        Category category = request.Adapt<Category>();

        _categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        await _cacheService.InvalidatePrefixAsync(CacheKeyPrefix);

        _logger.LogInformation("Category {CategoryId} ({CategoryName}) created", category.Id, category.Name);

        return Result<string>.Success("Category created.", 201);
    }

    // Update Category
    public async Task<Result<string>> UpdateAsync(CategoryUpdateDto request, CancellationToken cancellationToken)
    {
        Category? category = await _categories.FindAsync([request.Id], cancellationToken);

        if (category is null)
            return Result<string>.Failure(404, ErrorMessages.Category.NotFound);

        if (request.Name != category.Name)
        {
            bool isExists = await _categories.AnyAsync(c => c.Name.Equals(request.Name), cancellationToken);

            if (isExists)
                return Result<string>.Failure(409, ErrorMessages.Category.AlreadyExists);
        }

        request.Adapt(category);

        if (request.RowVersion is not null)
            _context.Entry(category).Property(x => x.RowVersion).OriginalValue = request.RowVersion;

        _categories.Update(category);
        await _context.SaveChangesAsync(cancellationToken);

        await _cacheService.InvalidatePrefixAsync(CacheKeyPrefix);

        _logger.LogInformation("Category {CategoryId} updated", category.Id);

        return "Category updated.";
    }


    // Delete Category
    public async Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Category? category = await _categories.FindAsync(id, cancellationToken);

        if (category is null)
            return Result<string>.Failure(404, ErrorMessages.Category.NotFound);

        _categories.Remove(category);

        await _context.SaveChangesAsync(cancellationToken);

        await _cacheService.InvalidatePrefixAsync(CacheKeyPrefix);

        _logger.LogInformation("Category {CategoryId} deleted", category.Id);

        return "Category deleted.";
    }

}

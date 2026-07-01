using Mapster;
using Microsoft.EntityFrameworkCore;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Categories.DataTransferObjects;
using Shoppy.Business.Extensions;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;

namespace Shoppy.Business.Categories;

public sealed class CategoryService(ApplicationDbContext _context) : ICategoryService
{

    private readonly DbSet<Category> _categories = _context.Set<Category>();

    // Get All Categories
    public async Task<Result<PaginationResultDto<CategoryResultDto>>> GetallAsync(PaginationRequestDto request, CancellationToken cancellationToken)
    {
        PaginationResultDto<CategoryResultDto> categories = await _categories
            .AsNoTracking()
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
            .OrderBy(c => c.Name)
            .WithPagination(request, cancellationToken);

        return categories;
    }

    // Get Category By Id

    public async Task<Result<CategoryResultDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        Category? category = await _categories.FindAsync(id, cancellationToken);

        if (category is null)
            return Result<CategoryResultDto>.Failure(404, "Category not found.");

        var categoryResult = category.Adapt<Result<CategoryResultDto>>();

        return categoryResult;
    }

    // Create Category

    public async Task<Result<string>> CreateAsync(CategoryCreateDto request, CancellationToken cancellationToken)
    {
        bool isExists = await _categories.AnyAsync(c => c.Name.Equals(request.Name), cancellationToken);

        if (isExists)
            return Result<string>.Failure(409, "Category is already exists.");

        Category category = request.Adapt<Category>();

        _categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return "Category created.";
    }

    // Update Category
    public async Task<Result<string>> UpdateAsync(CategoryUpdateDto request, CancellationToken cancellationToken)
    {
        Category? category = await _categories.FindAsync([request.Id], cancellationToken);

        if (category is null)
            return Result<string>.Failure(404, "Category not found.");

        if (request.Name != category.Name)
        {
            bool isExists = await _categories.AnyAsync(c => c.Name.Equals(request.Name), cancellationToken);

            if (isExists)
                return Result<string>.Failure(409, "Category is already exists.");
        }

        request.Adapt(category);

        _categories.Update(category);
        await _context.SaveChangesAsync(cancellationToken);
        return "Category updated.";
    }


    // Delete Category
    public async Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Category? category = await _categories.FindAsync(id, cancellationToken);

        if (category is null)
            return Result<string>.Failure(404, "Category not found.");

        _categories.Remove(category);

        await _context.SaveChangesAsync(cancellationToken);

        return "Category deleted.";
    }

}

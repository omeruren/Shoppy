using Mapster;
using Microsoft.EntityFrameworkCore;
using Shoppy.Business.DataTransferObjects;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;

namespace Shoppy.Business.Categories;

public sealed class CategoryService(ApplicationDbContext _context) : ICategoryService
{

    private readonly DbSet<Category> _categories = _context.Set<Category>();
    public async Task<List<CategoryResultDto>> GetallAsync(CancellationToken cancellationToken)
    {
        List<Category> list = await _categories
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var categories = list.Adapt<List<CategoryResultDto>>();

        return categories;
    }

    public async Task<CategoryResultDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        Category? category = await _categories.FindAsync(id, cancellationToken) ?? throw new ArgumentException("Category not found.");

        var categoryResult = category.Adapt<CategoryResultDto>();

        return categoryResult;
    }

    public async Task<string> CreateAsync(CategoryCreateDto request, CancellationToken cancellationToken)
    {
        bool isExists = await _categories.AnyAsync(c => c.Name.Equals(request.Name), cancellationToken);

        if (isExists)
            throw new ArgumentException("Category is already exists.");

        Category category = request.Adapt<Category>();

        _categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return "Category created.";
    }

    // Update Category
    public async Task<string> UpdateAsync(CategoryUpdateDto request, CancellationToken cancellationToken)
    {
        Category? category = await _categories.FindAsync([request.Id], cancellationToken) ?? throw new ArgumentException("Category not found");

        if (request.Name != category.Name)
        {
            bool isExists = await _categories.AnyAsync(c => c.Name.Equals(request.Name), cancellationToken);

            if (isExists)
                return "Category is already exists.";
        }

        request.Adapt(category);

        _categories.Update(category);
        await _context.SaveChangesAsync(cancellationToken);
        return "Category updated.";
    }


    // Delete Category
    public async Task<string> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Category? category = await _categories.FindAsync(id, cancellationToken) ?? throw new ArgumentException("Category not found.");

        _categories.Remove(category);

        await _context.SaveChangesAsync(cancellationToken);

        return "Category deleted.";
    }

}

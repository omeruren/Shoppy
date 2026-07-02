using Mapster;
using Microsoft.EntityFrameworkCore;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Extensions;
using Shoppy.Business.Products.DataTransferObjects;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;

namespace Shoppy.Business.Products;

public sealed class ProductService(ApplicationDbContext _context) : IProductService
{
    private readonly DbSet<Product> _products = _context.Set<Product>();


    // Get All Products
    public async Task<Result<PaginationResultDto<ProductResultDto>>> GetAllAsync(PaginationRequestDto request, CancellationToken cancellationToken)
    {
        return await _products
            .AsNoTracking()
            .LeftJoin(_context.Categories, p => p.CategoryId, p => p.Id, (product, category) => new { product, category })
            .Select(s => new ProductResultDto
            {
                Id = s.product.Id,
                Name = s.product.Name,
                Description = s.product.Description,
                CategoryId = s.product.CategoryId,
                CategoryName = s.category!.Name,

                CreatedAt = s.product.CreatedAt,
                UpdatedAt = s.product.UpdatedAt,
                DeletedAt = s.product.DeletedAt,
                IsDeleted = s.product.IsDeleted
            })
            .OrderBy(p => p.Name)
            .WithPagination(request, cancellationToken);

    }

    // Get Product By Id
    public async Task<Result<ProductResultDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        ProductResultDto? product = await _products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .LeftJoin(_context.Categories, p => p.CategoryId, p => p.Id, (product, category) => new { product, category })
            .Select(s => new ProductResultDto
            {
                Id = s.product.Id,
                Name = s.product.Name,
                Description = s.product.Description,
                CategoryId = s.product.CategoryId,
                CategoryName = s.category!.Name,

                CreatedAt = s.product.CreatedAt,
                UpdatedAt = s.product.UpdatedAt,
                DeletedAt = s.product.DeletedAt,
                IsDeleted = s.product.IsDeleted
            })
            .OrderBy(p => p.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
            return Result<ProductResultDto>.Failure(404, "Product not found.");

        return product;
    }

    // Create Product
    public async Task<Result<string>> CreateAsync(ProductCreateDto request, CancellationToken cancellationToken)
    {
        bool isExists = await _products.AnyAsync(p => p.Name.Equals(request.Name), cancellationToken);

        if (isExists)
            return Result<string>.Failure(409, "Product is already exists.");

        var product = request.Adapt<Product>();

        _products.Add(product);

        await _context.SaveChangesAsync(cancellationToken);
        return "Product Created.";
    }

    // Update Product
    public async Task<Result<string>> UpdateAsync(ProductUpdateDto request, CancellationToken cancellationToken)
    {
        Product? product = await _products.FindAsync([request.Id], cancellationToken);

        if (product is null)
            return Result<string>.Failure(404, "Product not found.");

        if (product.Name != request.Name)
        {

            bool isExists = await _products.AnyAsync(p => p.Name.Equals(request.Name), cancellationToken);

            if (isExists)
                return Result<string>.Failure(409, "Product is already exists.");
        }

        request.Adapt(product);

        if (request.RowVersion is not null)
            _context.Entry(product).Property(x => x.RowVersion).OriginalValue = request.RowVersion;


        _products.Update(product);
        await _context.SaveChangesAsync(cancellationToken);

        return "Product updated.";
    }


    public async Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Product? product = await _products.FindAsync([id], cancellationToken);

        if (product is null)
            return Result<string>.Failure(404, "Product not found.");

        _products.Remove(product);

        await _context.SaveChangesAsync(cancellationToken);

        return "Product not found.";
    }
}
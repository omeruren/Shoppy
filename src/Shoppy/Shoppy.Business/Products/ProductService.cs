using Mapster;
using Microsoft.EntityFrameworkCore;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Caching;
using Shoppy.Business.Extensions;
using Shoppy.Business.Products.DataTransferObjects;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;

namespace Shoppy.Business.Products;

public sealed class ProductService(ApplicationDbContext _context, ICacheService _cacheService) : IProductService
{
    private const string CacheKeyPrefix = "products";
    private readonly DbSet<Product> _products = _context.Set<Product>();

    // Get All Products
    public async Task<Result<PaginationResultDto<ProductResultDto>>> GetAllAsync(PaginationRequestDto request, CancellationToken cancellationToken)
    {
        return await _cacheService.GetOrCreateAsync(CacheKeyPrefix, request.ToCacheKey(CacheKeyPrefix), async () =>
        {
            return await _products
                .AsNoTracking()
                .Where(p => string.IsNullOrWhiteSpace(request.SearchTerm)
                    || p.Name.Contains(request.SearchTerm)
                    || (p.Description != null && p.Description.Contains(request.SearchTerm)))
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
        }, TimeSpan.FromMinutes(5));
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
            return Result<ProductResultDto>.Failure(404, ErrorMessages.Product.NotFound);

        return product;
    }

    // Create Product
    public async Task<Result<string>> CreateAsync(ProductCreateDto request, CancellationToken cancellationToken)
    {
        bool isExists = await _products.AnyAsync(p => p.Name.Equals(request.Name), cancellationToken);

        if (isExists)
            return Result<string>.Failure(409, ErrorMessages.Product.AlreadyExists);

        var product = request.Adapt<Product>();

        _products.Add(product);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Broad catch: Product.Name has a single unique index, so a concurrent duplicate-name
            // insert racing past the AnyAsync check above is the overwhelmingly likely cause here.
            // A genuine FK violation (invalid CategoryId) would also land here and be misreported
            // as a 409 rather than a more specific error — accepted tradeoff given the simpler check.
            return Result<string>.Failure(409, ErrorMessages.Product.AlreadyExists);
        }

        _cacheService.InvalidatePrefix(CacheKeyPrefix);

        return Result<string>.Success("Product created.", 201);
    }

    // Update Product
    public async Task<Result<string>> UpdateAsync(ProductUpdateDto request, CancellationToken cancellationToken)
    {
        Product? product = await _products.FindAsync([request.Id], cancellationToken);

        if (product is null)
            return Result<string>.Failure(404, ErrorMessages.Product.NotFound);

        if (product.Name != request.Name)
        {

            bool isExists = await _products.AnyAsync(p => p.Name.Equals(request.Name), cancellationToken);

            if (isExists)
                return Result<string>.Failure(409, ErrorMessages.Product.AlreadyExists);
        }

        request.Adapt(product);

        if (request.RowVersion is not null)
            _context.Entry(product).Property(x => x.RowVersion).OriginalValue = request.RowVersion;


        _products.Update(product);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result<string>.Failure(409, ErrorMessages.Product.AlreadyExists);
        }

        _cacheService.InvalidatePrefix(CacheKeyPrefix);

        return "Product updated.";
    }


    public async Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Product? product = await _products.FindAsync([id], cancellationToken);

        if (product is null)
            return Result<string>.Failure(404, ErrorMessages.Product.NotFound);

        _products.Remove(product);

        await _context.SaveChangesAsync(cancellationToken);

        _cacheService.InvalidatePrefix(CacheKeyPrefix);

        return "Product deleted.";
    }
}

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shoppy.Business.Caching;
using Shoppy.Business.Extensions;
using Shoppy.Business.Products;
using Shoppy.Business.Products.DataTransferObjects;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;
using Shoppy.UnitTests.TestDoubles;
using System.Security.Claims;

namespace Shoppy.UnitTests.Services;

public class ProductServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly ProductService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public ProductServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, _userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        httpContext.User = new ClaimsPrincipal(identity);
        httpContextAccessor.HttpContext.Returns(httpContext);

        _context = new ApplicationDbContext(options, httpContextAccessor);

        _cacheService = new NoOpCacheService();

        _service = new ProductService(_context, _cacheService, NullLogger<ProductService>.Instance);
    }

    private async Task<Category> SeedCategoryAsync(string name = "Electronics")
    {
        var category = new Category { Name = name };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    private async Task<Product> SeedProductAsync(string name = "Widget", Guid? categoryId = null)
    {
        var category = categoryId ?? (await SeedCategoryAsync()).Id;
        var product = new Product { Name = name, Price = 9.99m, CategoryId = category };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    // ─────────────────────────────────────────────
    //  GetByIdAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_Should_Return_Product_When_Exists()
    {
        var product = await SeedProductAsync();

        var result = await _service.GetByIdAsync(product.Id, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Widget");
        result.Data.Price.Should().Be(9.99m);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Fail_When_Product_Does_Not_Exist()
    {
        var result = await _service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.ErrorMessages.Should().Contain("Product not found.");
    }

    // ─────────────────────────────────────────────
    //  GetAllAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_Should_Return_Paginated_Products()
    {
        var category = await SeedCategoryAsync();
        await SeedProductAsync("A", category.Id);
        await SeedProductAsync("B", category.Id);
        await SeedProductAsync("C", category.Id);

        var request = new PaginationRequestDto(1, 10, string.Empty);

        var result = await _service.GetAllAsync(request, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data!.TotalCount.Should().Be(3);
        result.Data.Data.Should().OnlyContain(p => p.Price == 9.99m);
    }

    [Fact]
    public async Task GetAllAsync_Should_Filter_By_SearchTerm()
    {
        var category = await SeedCategoryAsync();
        await SeedProductAsync("Red Widget", category.Id);
        await SeedProductAsync("Blue Gadget", category.Id);

        var request = new PaginationRequestDto(1, 10, "Widget");

        var result = await _service.GetAllAsync(request, CancellationToken.None);

        result.Data!.TotalCount.Should().Be(1);
        result.Data.Data.Single().Name.Should().Be("Red Widget");
    }

    [Fact]
    public async Task GetAllAsync_Should_Clamp_PageSize_To_Max()
    {
        var category = await SeedCategoryAsync();
        await SeedProductAsync("A", category.Id);

        var request = new PaginationRequestDto(1, 1_000_000, string.Empty);

        var result = await _service.GetAllAsync(request, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data!.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task GetAllAsync_Should_Clamp_PageNumber_To_Minimum_One()
    {
        var category = await SeedCategoryAsync();
        await SeedProductAsync("A", category.Id);

        var request = new PaginationRequestDto(-5, 10, string.Empty);

        var result = await _service.GetAllAsync(request, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data!.PageNumber.Should().Be(1);
    }

    // ─────────────────────────────────────────────
    //  CreateAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_Should_Create_Product_When_Not_Exists()
    {
        var category = await SeedCategoryAsync();
        var request = new ProductCreateDto("New Product", "desc", 19.99m, category.Id);

        var result = await _service.CreateAsync(request, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.StatusCode.Should().Be(201);

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Name == "New Product");
        product.Should().NotBeNull();
        product!.CreatedBy.Should().Be(_userId);
    }

    [Fact]
    public async Task CreateAsync_Should_Return_409_When_Name_Already_Exists()
    {
        var category = await SeedCategoryAsync();
        await SeedProductAsync("Duplicate", category.Id);

        var request = new ProductCreateDto("Duplicate", null, 5m, category.Id);

        var result = await _service.CreateAsync(request, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.ErrorMessages.Should().Contain("Product already exists.");
    }

    // ─────────────────────────────────────────────
    //  UpdateAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_Should_Update_Product_When_Exists()
    {
        var product = await SeedProductAsync();
        var request = new ProductUpdateDto(product.Id, "Updated Name", "desc", 29.99m, product.CategoryId);

        var result = await _service.UpdateAsync(request, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();

        var updated = await _context.Products.FindAsync(product.Id);
        updated!.Name.Should().Be("Updated Name");
        updated.UpdatedBy.Should().Be(_userId);
    }

    [Fact]
    public async Task UpdateAsync_Should_Fail_When_Product_Does_Not_Exist()
    {
        var request = new ProductUpdateDto(Guid.NewGuid(), "Name", null, 1m, Guid.NewGuid());

        var result = await _service.UpdateAsync(request, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.ErrorMessages.Should().Contain("Product not found.");
    }

    [Fact]
    public async Task UpdateAsync_Should_Return_409_When_Renaming_To_Existing_Name()
    {
        var category = await SeedCategoryAsync();
        var product = await SeedProductAsync("Original", category.Id);
        await SeedProductAsync("Taken", category.Id);

        var request = new ProductUpdateDto(product.Id, "Taken", null, product.Price, category.Id);

        var result = await _service.UpdateAsync(request, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    // ─────────────────────────────────────────────
    //  DeleteAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_Should_SoftDelete_Product_When_Exists()
    {
        var product = await SeedProductAsync();

        var result = await _service.DeleteAsync(product.Id, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();

        var deleted = await _context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == product.Id);
        deleted!.IsDeleted.Should().BeTrue();
        deleted.DeletedBy.Should().Be(_userId);
    }

    [Fact]
    public async Task DeleteAsync_Should_Fail_When_Product_Does_Not_Exist()
    {
        var result = await _service.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.ErrorMessages.Should().Contain("Product not found.");
    }
}

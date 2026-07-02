using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Shoppy.Business.Categories;
using Shoppy.Business.Categories.DataTransferObjects;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;
using System.Security.Claims;

namespace Shoppy.UnitTests.Services;

public class CategoryServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly CategoryService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public CategoryServiceTests()
    {
        // Setup InMemory database options
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Stub HttpContextAccessor for auditing properties (CreatedBy, etc.)
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, _userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;
        httpContextAccessor.HttpContext.Returns(httpContext);

        _context = new ApplicationDbContext(options, httpContextAccessor);

        // Stub MemoryCache
        _cache = Substitute.For<IMemoryCache>();
        // Return null by default for cache misses
        object? cacheEntry = null;
        _cache.TryGetValue(Arg.Any<object>(), out cacheEntry).Returns(false);

        _service = new CategoryService(_context, _cache);
    }

    [Fact]
    public async Task CreateAsync_Should_Create_Category_When_Not_Exists()
    {
        // Arrange
        var request = new CategoryCreateDto("Electronics");

        // Act
        var result = await _service.CreateAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().Be("Category created.");

        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Electronics");
        category.Should().NotBeNull();
        category!.CreatedBy.Should().Be(_userId);
    }

    [Fact]
    public async Task CreateAsync_Should_Fail_When_Category_Already_Exists()
    {
        // Arrange
        var category = new Category { Name = "Books" };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var request = new CategoryCreateDto("Books");

        // Act
        var result = await _service.CreateAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.ErrorMessages.Should().Contain("Category already exists.");
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Category_When_Exists()
    {
        // Arrange
        var category = new Category { Name = "Automotive" };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(category.Id, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Automotive");
    }

    [Fact]
    public async Task GetByIdAsync_Should_Fail_When_Category_Does_Not_Exist()
    {
        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.ErrorMessages.Should().Contain("Category not found.");
    }

    [Fact]
    public async Task UpdateAsync_Should_Update_Category_When_Exists()
    {
        // Arrange
        var category = new Category { Name = "Garden" };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var request = new CategoryUpdateDto(category.Id, "Outdoors");

        // Act
        var result = await _service.UpdateAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().Be("Category updated.");

        var updated = await _context.Categories.FindAsync(category.Id);
        updated!.Name.Should().Be("Outdoors");
        updated.UpdatedBy.Should().Be(_userId);
    }

    [Fact]
    public async Task DeleteAsync_Should_SoftDelete_Category_When_Exists()
    {
        // Arrange
        var category = new Category { Name = "Toys" };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(category.Id, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().Be("Category deleted.");

        // Clean query filters (soft delete filters) to check if record is soft-deleted
        var deleted = await _context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == category.Id);
        deleted!.IsDeleted.Should().BeTrue();
        deleted.DeletedBy.Should().Be(_userId);
    }
}

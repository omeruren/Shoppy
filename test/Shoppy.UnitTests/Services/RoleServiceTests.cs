using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shoppy.Business.Caching;
using Shoppy.Business.Extensions;
using Shoppy.Business.Permissions;
using Shoppy.Business.Roles;
using Shoppy.Business.Roles.DataTransferObjects;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;
using Shoppy.UnitTests.TestDoubles;

namespace Shoppy.UnitTests.Services;

public class RoleServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly RoleService _service;

    public RoleServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContextAccessor.HttpContext.Returns(httpContext);

        _context = new ApplicationDbContext(options, httpContextAccessor);

        ICacheService cacheService = new NoOpCacheService();

        _service = new RoleService(_context, cacheService, NullLogger<RoleService>.Instance);
    }

    private async Task<Role> SeedRoleAsync(string name = "Admin")
    {
        var role = new Role { Name = name };
        _context.AppRoles.Add(role);
        await _context.SaveChangesAsync();
        return role;
    }

    // ─────────────────────────────────────────────
    //  GetAllAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_Should_Return_All_Roles_Ordered_By_Name()
    {
        await SeedRoleAsync("Customer");
        await SeedRoleAsync("Admin");

        var result = await _service.GetAllAsync(new PaginationRequestDto(1, 10, string.Empty), CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data!.Data.Should().HaveCount(2);
        result.Data.Data.Select(r => r.Name).Should().ContainInOrder("Admin", "Customer");
    }

    [Fact]
    public async Task GetAllAsync_Should_Filter_By_SearchTerm()
    {
        await SeedRoleAsync("Customer");
        await SeedRoleAsync("Admin");

        var result = await _service.GetAllAsync(new PaginationRequestDto(1, 10, "Admin"), CancellationToken.None);

        result.Data!.Data.Should().ContainSingle().Which.Name.Should().Be("Admin");
    }

    // ─────────────────────────────────────────────
    //  GetByIdAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_Should_Return_Role_When_Exists()
    {
        var role = await SeedRoleAsync();

        var result = await _service.GetByIdAsync(role.Id, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data!.Name.Should().Be("Admin");
    }

    [Fact]
    public async Task GetByIdAsync_Should_Fail_When_Not_Found()
    {
        var result = await _service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    // ─────────────────────────────────────────────
    //  CreateAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_Should_Create_Role_When_Name_Not_Taken()
    {
        var request = new RoleCreateDto("Manager");

        var result = await _service.CreateAsync(request, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.StatusCode.Should().Be(201);

        var role = await _context.AppRoles.FirstOrDefaultAsync(r => r.Name == "Manager");
        role.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_Should_Return_409_When_Name_Already_Exists()
    {
        await SeedRoleAsync("Manager");
        var request = new RoleCreateDto("Manager");

        var result = await _service.CreateAsync(request, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    // ─────────────────────────────────────────────
    //  UpdateAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_Should_Update_Role_When_Exists()
    {
        var role = await SeedRoleAsync("Manager");
        var request = new RoleUpdateDto(role.Id, "Senior Manager");

        var result = await _service.UpdateAsync(request, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();

        var updated = await _context.AppRoles.FindAsync(role.Id);
        updated!.Name.Should().Be("Senior Manager");
    }

    [Fact]
    public async Task UpdateAsync_Should_Fail_When_Not_Found()
    {
        var request = new RoleUpdateDto(Guid.NewGuid(), "Ghost Role");

        var result = await _service.UpdateAsync(request, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateAsync_Should_Return_409_When_Renaming_To_Existing_Name()
    {
        var role = await SeedRoleAsync("Manager");
        await SeedRoleAsync("Taken");

        var request = new RoleUpdateDto(role.Id, "Taken");

        var result = await _service.UpdateAsync(request, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    // ─────────────────────────────────────────────
    //  DeleteAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_Should_Delete_Role_When_Exists()
    {
        var role = await SeedRoleAsync("Manager");

        var result = await _service.DeleteAsync(role.Id, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();

        var deleted = await _context.AppRoles.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == role.Id);
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_Should_Fail_When_Not_Found()
    {
        var result = await _service.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    // ─────────────────────────────────────────────
    //  GetPermissionsAsync / UpdatePermissionsAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetPermissionsAsync_Should_Fail_When_Role_Not_Found()
    {
        var result = await _service.GetPermissionsAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetPermissionsAsync_Should_Return_Empty_List_When_Role_Has_No_Permissions()
    {
        var role = await SeedRoleAsync("Manager");

        var result = await _service.GetPermissionsAsync(role.Id, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdatePermissionsAsync_Should_Assign_Valid_Permissions()
    {
        var role = await SeedRoleAsync("Manager");

        var result = await _service.UpdatePermissionsAsync(
            role.Id, [Permissions.Products.Read, Permissions.Categories.Read], CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();

        var permissions = await _service.GetPermissionsAsync(role.Id, CancellationToken.None);
        permissions.Data.Should().BeEquivalentTo([Permissions.Products.Read, Permissions.Categories.Read]);
    }

    [Fact]
    public async Task UpdatePermissionsAsync_Should_Reject_Unknown_Permission()
    {
        var role = await SeedRoleAsync("Manager");

        var result = await _service.UpdatePermissionsAsync(
            role.Id, ["NotARealPermission.Read"], CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task UpdatePermissionsAsync_Should_Fail_When_Role_Not_Found()
    {
        var result = await _service.UpdatePermissionsAsync(
            Guid.NewGuid(), [Permissions.Products.Read], CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdatePermissionsAsync_Should_Clear_Permissions_When_Given_Empty_List()
    {
        var role = await SeedRoleAsync("Manager");
        await _service.UpdatePermissionsAsync(role.Id, [Permissions.Products.Read], CancellationToken.None);

        var result = await _service.UpdatePermissionsAsync(role.Id, [], CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();

        var permissions = await _service.GetPermissionsAsync(role.Id, CancellationToken.None);
        permissions.Data.Should().BeEmpty();
    }
}

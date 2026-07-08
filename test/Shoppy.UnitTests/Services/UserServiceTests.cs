using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shoppy.Business.Extensions;
using Shoppy.Business.Users;
using Shoppy.Business.Users.DataTransferObjects;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;

namespace Shoppy.UnitTests.Services;

public class UserServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly UserService _service;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContextAccessor.HttpContext.Returns(httpContext);

        _context = new ApplicationDbContext(options, httpContextAccessor);

        var userStore = Substitute.For<IUserStore<User>>();
        _userManager = Substitute.For<UserManager<User>>(userStore, null, null, null, null, null, null, null, null);

        // UserService queries UserManager.Users, whose real implementation (UserStore) is
        // backed by the DbContext's Users DbSet — wiring the substitute's Users property to
        // the same EF InMemory-backed queryable keeps CountAsync/ToListAsync (used by
        // WithPagination) working, which a plain List<T>.AsQueryable() would not support.
        _userManager.Users.Returns(_context.Users);

        _service = new UserService(_userManager, NullLogger<UserService>.Instance);
    }

    private async Task<User> SeedUserAsync(string userName = "johndoe", bool isDeleted = false)
    {
        var user = User.Create("John", "Doe", userName, $"{userName}@example.com");
        user.Id = Guid.NewGuid();
        user.IsDeleted = isDeleted;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private void SetupFindById(User user) =>
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(Task.FromResult<User?>(user));

    // ─────────────────────────────────────────────
    //  GetByIdAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_Should_Return_Profile_When_Exists()
    {
        var user = await SeedUserAsync();
        SetupFindById(user);

        var result = await _service.GetByIdAsync(user.Id, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data!.UserName.Should().Be(user.UserName);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Fail_When_Not_Found()
    {
        _userManager.FindByIdAsync(Arg.Any<string>()).Returns(Task.FromResult<User?>(null));

        var result = await _service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Fail_When_User_Is_SoftDeleted()
    {
        var user = await SeedUserAsync(isDeleted: true);
        SetupFindById(user);

        var result = await _service.GetByIdAsync(user.Id, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    // ─────────────────────────────────────────────
    //  GetAllAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_Should_Exclude_SoftDeleted_Users()
    {
        await SeedUserAsync("active1");
        await SeedUserAsync("active2");
        await SeedUserAsync("deleted1", isDeleted: true);

        var request = new PaginationRequestDto(1, 10, string.Empty);

        var result = await _service.GetAllAsync(request, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data!.TotalCount.Should().Be(2);
        result.Data.Data.Should().OnlyContain(u => u.UserName != "deleted1");
    }

    [Fact]
    public async Task GetAllAsync_Should_Filter_By_SearchTerm()
    {
        await SeedUserAsync("johndoe");
        await SeedUserAsync("janedoe");

        var request = new PaginationRequestDto(1, 10, "john");

        var result = await _service.GetAllAsync(request, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data!.TotalCount.Should().Be(1);
        result.Data.Data.Should().OnlyContain(u => u.UserName == "johndoe");
    }

    // ─────────────────────────────────────────────
    //  CreateAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_Should_Create_When_Email_Not_Taken()
    {
        var request = new UserCreateDto("Jane", "Doe", "janedoe", "jane@example.com", "Password123!");

        _userManager.CreateAsync(Arg.Any<User>(), request.Password).Returns(Task.FromResult(IdentityResult.Success));

        var result = await _service.CreateAsync(request, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreateAsync_Should_Return_409_When_Email_Already_Taken()
    {
        await SeedUserAsync("existing");
        var request = new UserCreateDto("Jane", "Doe", "janedoe", "existing@example.com", "Password123!");

        var result = await _service.CreateAsync(request, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task CreateAsync_Should_Return_400_When_Identity_Creation_Fails()
    {
        var request = new UserCreateDto("Jane", "Doe", "janedoe", "jane@example.com", "weak");

        _userManager.CreateAsync(Arg.Any<User>(), request.Password)
            .Returns(Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "Password too weak." })));

        var result = await _service.CreateAsync(request, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessages.Should().Contain("Password too weak.");
    }

    // ─────────────────────────────────────────────
    //  UpdateAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_Should_Update_When_Exists()
    {
        var user = await SeedUserAsync();
        SetupFindById(user);
        _userManager.UpdateAsync(user).Returns(Task.FromResult(IdentityResult.Success));

        var request = new UserUpdateDto(user.Id, "Updated", "Name", "updateduser", "updated@example.com");

        var result = await _service.UpdateAsync(request, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        user.FirstName.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateAsync_Should_Fail_When_Not_Found()
    {
        _userManager.FindByIdAsync(Arg.Any<string>()).Returns(Task.FromResult<User?>(null));

        var request = new UserUpdateDto(Guid.NewGuid(), "A", "B", "c", "c@example.com");

        var result = await _service.UpdateAsync(request, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    // ─────────────────────────────────────────────
    //  DeleteAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_Should_SoftDelete_When_User_Is_Active()
    {
        var user = await SeedUserAsync();
        SetupFindById(user);
        _userManager.UpdateAsync(user).Returns(Task.FromResult(IdentityResult.Success));

        var result = await _service.DeleteAsync(user.Id, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        user.IsDeleted.Should().BeTrue();
        await _userManager.DidNotReceive().DeleteAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task DeleteAsync_Should_HardDelete_When_User_Already_SoftDeleted()
    {
        var user = await SeedUserAsync(isDeleted: true);
        SetupFindById(user);
        _userManager.DeleteAsync(user).Returns(Task.FromResult(IdentityResult.Success));

        var result = await _service.DeleteAsync(user.Id, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        await _userManager.Received(1).DeleteAsync(user);
    }

    [Fact]
    public async Task DeleteAsync_Should_Fail_When_Not_Found()
    {
        _userManager.FindByIdAsync(Arg.Any<string>()).Returns(Task.FromResult<User?>(null));

        var result = await _service.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    // ─────────────────────────────────────────────
    //  Self-service
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetProfileAsync_Should_Return_Own_Profile()
    {
        var user = await SeedUserAsync();
        SetupFindById(user);

        var result = await _service.GetProfileAsync(user.Id, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data!.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task UpdateSelfAsync_Should_Update_Name_But_Not_Email()
    {
        var user = await SeedUserAsync();
        var originalEmail = user.Email;
        SetupFindById(user);
        _userManager.UpdateAsync(user).Returns(Task.FromResult(IdentityResult.Success));

        var request = new UserUpdateSelfDto("NewFirst", "NewLast", "newusername");

        var result = await _service.UpdateSelfAsync(user.Id, request, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        user.FirstName.Should().Be("NewFirst");
        user.Email.Should().Be(originalEmail);
    }

    [Fact]
    public async Task ChangePasswordAsync_Should_Fail_When_Confirmation_Does_Not_Match()
    {
        var user = await SeedUserAsync();

        var request = new ChangePasswordDto("OldPass1!", "NewPass1!", "Mismatch1!");

        var result = await _service.ChangePasswordAsync(user.Id, request, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ChangePasswordAsync_Should_Succeed_When_Current_Password_Is_Correct()
    {
        var user = await SeedUserAsync();
        SetupFindById(user);
        _userManager.ChangePasswordAsync(user, "OldPass1!", "NewPass1!")
            .Returns(Task.FromResult(IdentityResult.Success));

        var request = new ChangePasswordDto("OldPass1!", "NewPass1!", "NewPass1!");

        var result = await _service.ChangePasswordAsync(user.Id, request, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
    }
}

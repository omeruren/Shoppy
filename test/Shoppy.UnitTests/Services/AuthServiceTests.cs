using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shoppy.Business.Auth;
using Shoppy.Business.Auth.DataTransferObjects;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Options;
using Shoppy.Business.Services;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;

namespace Shoppy.UnitTests.Services;

public class AuthServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly AuthService _service;
    private readonly User _user;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContextAccessor.HttpContext.Returns(httpContext);

        _context = new ApplicationDbContext(options, httpContextAccessor);

        _user = User.Create("John", "Doe", "johndoe", "john@example.com");
        _user.Id = Guid.NewGuid();

        // RefreshToken.User is a required navigation (non-nullable UserId FK), so .Include(rt => rt.User)
        // in AuthService compiles to an inner join — the user must actually exist in the DbContext,
        // not just be returned by the mocked UserManager, or every RefreshTokenAsync lookup comes back null.
        _context.Users.Add(_user);
        _context.SaveChanges();

        var userStore = Substitute.For<IUserStore<User>>();
        _userManager = Substitute.For<UserManager<User>>(userStore, null, null, null, null, null, null, null, null);
        _userManager.FindByNameAsync(_user.UserName!).Returns(Task.FromResult<User?>(_user));
        _userManager.CheckPasswordAsync(_user, Arg.Any<string>()).Returns(Task.FromResult(true));

        var jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SecretKey = "this-is-a-super-secret-test-key-with-enough-length-1234567890"
        });
        var jwtProvider = new JwtProvider(jwtOptions);

        var emailService = Substitute.For<IEmailService>();

        _service = new AuthService(_userManager, jwtProvider, _context, emailService, NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task LoginAsync_Should_Start_New_Token_Family()
    {
        var result = await _service.LoginAsync(new LoginRequestDto(_user.UserName!, "password"), CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();

        var stored = await _context.RefreshTokens.SingleAsync(rt => rt.Token == result.Data!.RefreshToken);
        stored.FamilyId.Should().NotBe(Guid.Empty);
        stored.IsRevoked.Should().BeFalse();
        stored.ReplacedByToken.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_Should_Reset_AccessFailedCount_On_Successful_Login()
    {
        var result = await _service.LoginAsync(new LoginRequestDto(_user.UserName!, "password"), CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        await _userManager.Received(1).ResetAccessFailedCountAsync(_user);
    }

    [Fact]
    public async Task LoginAsync_Should_Record_Failed_Attempt_When_Password_Is_Wrong()
    {
        _userManager.CheckPasswordAsync(_user, Arg.Any<string>()).Returns(Task.FromResult(false));

        var result = await _service.LoginAsync(new LoginRequestDto(_user.UserName!, "wrong-password"), CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(401);
        await _userManager.Received(1).AccessFailedAsync(_user);
        await _userManager.DidNotReceive().ResetAccessFailedCountAsync(_user);
    }

    [Fact]
    public async Task LoginAsync_Should_Fail_When_Account_Is_Locked_Out()
    {
        _userManager.IsLockedOutAsync(_user).Returns(Task.FromResult(true));

        var result = await _service.LoginAsync(new LoginRequestDto(_user.UserName!, "password"), CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(401);
        result.ErrorMessages.Should().Contain(ErrorMessages.Auth.AccountLockedOut);
        await _userManager.DidNotReceive().CheckPasswordAsync(_user, Arg.Any<string>());
    }

    [Fact]
    public async Task RefreshTokenAsync_Should_Rotate_Token_Within_Same_Family()
    {
        var login = await _service.LoginAsync(new LoginRequestDto(_user.UserName!, "password"), CancellationToken.None);
        var originalToken = await _context.RefreshTokens.SingleAsync(rt => rt.Token == login.Data!.RefreshToken);
        var originalFamilyId = originalToken.FamilyId;

        var refreshResult = await _service.RefreshTokenAsync(new RefreshTokenRequestDto(login.Data!.RefreshToken), CancellationToken.None);

        refreshResult.IsSuccessful.Should().BeTrue();

        var oldToken = await _context.RefreshTokens.SingleAsync(rt => rt.Id == originalToken.Id);
        oldToken.IsRevoked.Should().BeTrue();
        oldToken.ReplacedByToken.Should().Be(refreshResult.Data!.RefreshToken);

        var newToken = await _context.RefreshTokens.SingleAsync(rt => rt.Token == refreshResult.Data!.RefreshToken);
        newToken.FamilyId.Should().Be(originalFamilyId);
        newToken.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshTokenAsync_Should_Revoke_Entire_Family_On_Reuse()
    {
        var login = await _service.LoginAsync(new LoginRequestDto(_user.UserName!, "password"), CancellationToken.None);
        var firstRefreshToken = login.Data!.RefreshToken;
        var originalToken = await _context.RefreshTokens.SingleAsync(rt => rt.Token == firstRefreshToken);
        var familyId = originalToken.FamilyId;

        // Rotate once (valid) — this revokes firstRefreshToken and issues a second, still-active one
        var firstRotation = await _service.RefreshTokenAsync(new RefreshTokenRequestDto(firstRefreshToken), CancellationToken.None);
        firstRotation.IsSuccessful.Should().BeTrue();

        // Reuse the already-revoked original token — simulates a stolen-token replay
        var reuseResult = await _service.RefreshTokenAsync(new RefreshTokenRequestDto(firstRefreshToken), CancellationToken.None);

        reuseResult.IsSuccessful.Should().BeFalse();
        reuseResult.StatusCode.Should().Be(401);

        var familyTokens = await _context.RefreshTokens.Where(rt => rt.FamilyId == familyId).ToListAsync();
        familyTokens.Should().OnlyContain(rt => rt.IsRevoked);
    }
}

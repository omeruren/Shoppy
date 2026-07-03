using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shoppy.Business.Auth.DataTransferObjects;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Roles.DataTransferObjects;
using Shoppy.Entity.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Shoppy.IntegrationTests;

public class AuthFlowIntegrationTests : IClassFixture<RelaxedAuthRateLimitWebApplicationFactory>
{
    private readonly RelaxedAuthRateLimitWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthFlowIntegrationTests(RelaxedAuthRateLimitWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    private async Task SeedUserAsync(string username, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var existing = await userManager.FindByNameAsync(username);
        if (existing is not null)
            return;

        var user = User.Create("Jane", "Doe", username, $"{username}@example.com");
        var result = await userManager.CreateAsync(user, password);
        result.Succeeded.Should().BeTrue();
    }

    private async Task<LoginResponseDto> LoginAsync(string username, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto(username, password));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<LoginResponseDto>>();
        result.Should().NotBeNull();
        result!.IsSuccessful.Should().BeTrue();

        return result.Data!;
    }

    // ─────────────────────────────────────────────
    //  Login + refresh rotation
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Login_With_Valid_Credentials_Should_Return_AccessToken_And_RefreshToken()
    {
        await SeedUserAsync("authflow_login", "Password123!");

        var login = await LoginAsync("authflow_login", "Password123!");

        login.AccessToken.Should().NotBeNullOrWhiteSpace();
        login.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RefreshToken_Should_Rotate_And_Return_New_Tokens()
    {
        await SeedUserAsync("authflow_rotate", "Password123!");
        var login = await LoginAsync("authflow_rotate", "Password123!");

        var refreshResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh", new RefreshTokenRequestDto(login.RefreshToken));

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<Result<LoginResponseDto>>();
        refreshResult.Should().NotBeNull();
        refreshResult!.IsSuccessful.Should().BeTrue();
        refreshResult.Data!.RefreshToken.Should().NotBe(login.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_Reuse_After_Rotation_Should_Revoke_Family_And_Reject_Subsequent_Refresh()
    {
        await SeedUserAsync("authflow_reuse", "Password123!");
        var login = await LoginAsync("authflow_reuse", "Password123!");

        // First rotation: consumes the original refresh token, issues a new one in the same family.
        var firstRefresh = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh", new RefreshTokenRequestDto(login.RefreshToken));
        firstRefresh.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstRefreshResult = await firstRefresh.Content.ReadFromJsonAsync<Result<LoginResponseDto>>();
        var rotatedToken = firstRefreshResult!.Data!.RefreshToken;

        // Reuse of the original (now-revoked) token should be detected as theft and
        // revoke the entire family — including the token that was just issued above.
        var reuseAttempt = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh", new RefreshTokenRequestDto(login.RefreshToken));
        reuseAttempt.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var secondRefreshAttempt = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh", new RefreshTokenRequestDto(rotatedToken));
        secondRefreshAttempt.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─────────────────────────────────────────────
    //  Permission enforcement
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Endpoint_Requiring_Permission_Should_Return_403_When_Authenticated_User_Lacks_Permission()
    {
        // A user with no role assignment is authenticated but carries no permission claims.
        await SeedUserAsync("authflow_norole", "Password123!");
        var login = await LoginAsync("authflow_norole", "Password123!");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/roles")
        {
            Content = JsonContent.Create(new RoleCreateDto("SomeNewRole"))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

using FluentAssertions;
using Shoppy.Business.Auth.DataTransferObjects;
using System.Net;
using System.Net.Http.Json;

namespace Shoppy.IntegrationTests;

// Runs against its own CustomWebApplicationFactory instance (own app boot, own
// rate-limiter singleton state) so flooding a global, non-partitioned limiter bucket
// here can't bleed into other test classes' legitimate auth requests.
public class RateLimitingIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RateLimitingIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_Should_Be_Rate_Limited_When_Requests_Exceed_The_Auth_Fixed_Window()
    {
        // auth-fixed limiter: 5 requests / 1s window, no queue, not partitioned by
        // caller — fire well past the limit inside the window and expect rejections.
        // ASP.NET Core's default rate-limiter rejection status is 503 (no
        // RejectionStatusCode configured for this policy), not 429.
        var badLogin = new LoginRequestDto("nonexistent-user", "wrong-password");

        var responses = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => _client.PostAsJsonAsync("/api/v1/auth/login", badLogin)));

        responses.Should().Contain(r => r.StatusCode == HttpStatusCode.ServiceUnavailable);
    }
}

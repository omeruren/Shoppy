using FluentAssertions;
using Shoppy.Business.Auth.DataTransferObjects;
using System.Net;
using System.Net.Http.Json;

namespace Shoppy.IntegrationTests;

// Runs against its own CustomWebApplicationFactory instance (own app boot, own
// rate-limiter singleton state) so flooding this bucket here can't bleed into other
// test classes' legitimate auth requests.
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
        // auth-fixed limiter: 5 requests / 1s window per client IP, no queue — TestServer
        // reports every in-memory request from the same client as the same loopback IP, so
        // firing well past the limit from this one client still exceeds its partition.
        // Rejections return 429 (RejectionStatusCode is explicitly configured in Program.cs).
        var badLogin = new LoginRequestDto("nonexistent-user", "wrong-password");

        var responses = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => _client.PostAsJsonAsync("/api/v1/auth/login", badLogin)));

        responses.Should().Contain(r => r.StatusCode == HttpStatusCode.TooManyRequests);
    }
}

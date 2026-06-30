using Carter;
using Shoppy.Business.Auth;
using Shoppy.Business.Auth.DataTransferObjects;

namespace Shoppy.WebAPI.Modules;

public sealed class AuthModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder builder)
    {
        var app = builder.MapGroup("/auth").WithTags("Auth");

        // LOGIN

        app.MapPost("login", async (
           LoginRequestDto request,
            IAuthService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.LoginAsync(request, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.StatusCode(result.StatusCode);
        });

        app.MapPost("refresh", async (
            RefreshTokenRequestDto request,
            IAuthService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.RefreshTokenAsync(request, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.Unauthorized();
        });
    }
}
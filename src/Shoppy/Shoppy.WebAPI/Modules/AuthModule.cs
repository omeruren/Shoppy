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
            IAuthService _service) =>
        {
            var result = await _service.LoginAsync(request);

            return result.IsSuccessful ? Results.Ok(result) : Results.StatusCode(result.StatusCode);
        });


        app.MapGet("/me", (HttpContext context) =>
        {
            return Results.Ok(new
            {
                context.User.Identity?.IsAuthenticated,
                Claims = context.User.Claims.Select(x => new
                {
                    x.Type,
                    x.Value
                })
            });
        }).RequireAuthorization();
    }
}
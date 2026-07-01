using Carter;
using Shoppy.Business.Auth;
using Shoppy.Business.Auth.DataTransferObjects;
using Shoppy.WebAPI.Filters;

namespace Shoppy.WebAPI.Modules;

public sealed class AuthModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder builder)
    {
        var app = builder
            .MapGroup("/auth")
            .WithTags("Auth")
            .RequireRateLimiting("auth-fixed");

        // LOGIN

        app.MapPost("login", async (
           LoginRequestDto request,
            IAuthService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.LoginAsync(request, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.StatusCode(result.StatusCode);
        });


        // REFRESH TOKEN

        app.MapPost("refresh", async (
            RefreshTokenRequestDto request,
            IAuthService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.RefreshTokenAsync(request, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.Unauthorized();
        });


        // FORGOT PASSWORD

        app.MapPost("forgot-password", async (
            ForgotPasswordRequestDto request,
            IAuthService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.ForgotPasswordAsync(request, cancellationToken);

            // always return 200 to prevent user enumeration attacks.

            return result.IsSuccessful ? Results.Ok(result) : Results.Problem(result.ErrorMessages?.FirstOrDefault(), statusCode: result.StatusCode);
        }).AddEndpointFilter<FluentValidationFilter<ForgotPasswordRequestDto>>();

        // RESET PASSWORD

        app.MapPost("reset-password", async (
            ResetPasswordRequestDto request,
            IAuthService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.ResetPasswordAsync(request, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.BadRequest(result);
        }).AddEndpointFilter<FluentValidationFilter<ResetPasswordRequestDto>>();

    }
}
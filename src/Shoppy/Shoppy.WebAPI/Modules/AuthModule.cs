using Asp.Versioning;
using Carter;
using Shoppy.Business.Auth;
using Shoppy.Business.Auth.DataTransferObjects;
using Shoppy.Business.BaseResult;
using Shoppy.WebAPI.Filters;

namespace Shoppy.WebAPI.Modules;

public sealed class AuthModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder builder)
    {

        var apiVersionSet = builder.NewApiVersionSet()
                    .HasApiVersion(new ApiVersion(1, 0))
                    .ReportApiVersions()
                    .Build();

        var app = builder.MapGroup("/api/v{version:apiVersion}/auth")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Auth")
            .RequireRateLimiting("auth-fixed");

        // LOGIN

        app.MapPost("login", async (
           LoginRequestDto request,
            IAuthService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.LoginAsync(request, cancellationToken);

            return result.ToHttpResult();
        });


        // REFRESH TOKEN

        app.MapPost("refresh", async (
            RefreshTokenRequestDto request,
            IAuthService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.RefreshTokenAsync(request, cancellationToken);

            return result.ToHttpResult();
        });


        // FORGOT PASSWORD

        app.MapPost("forgot-password", async (
            ForgotPasswordRequestDto request,
            IAuthService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.ForgotPasswordAsync(request, cancellationToken);

            // always return 200 to prevent user enumeration attacks.

            return result.ToHttpResult();
        }).AddEndpointFilter<FluentValidationFilter<ForgotPasswordRequestDto>>();

        // RESET PASSWORD

        app.MapPost("reset-password", async (
            ResetPasswordRequestDto request,
            IAuthService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.ResetPasswordAsync(request, cancellationToken);

            return result.ToHttpResult();
        }).AddEndpointFilter<FluentValidationFilter<ResetPasswordRequestDto>>();

    }
}
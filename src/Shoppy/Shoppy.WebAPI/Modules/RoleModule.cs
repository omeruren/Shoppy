using Asp.Versioning;
using Carter;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Roles;
using Shoppy.Business.Roles.DataTransferObjects;
using Shoppy.WebAPI.Filters;

namespace Shoppy.WebAPI.Modules;

public sealed class RoleModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder builder)
    {
        var apiVersionSet = builder.NewApiVersionSet()
          .HasApiVersion(new ApiVersion(1, 0))
          .ReportApiVersions()
          .Build();

        var app = builder
            .MapGroup("/api/v{version:apiVersion}/roles")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Roles")
            .RequireRateLimiting("fixed")
            .RequireAuthorization();

        // GET ALL ROLES
        app.MapGet(string.Empty, async (
            IRoleService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.GetAllAsync(cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.StatusCode(result.StatusCode);
        });

        // GET ROLE BY ID
        app.MapGet("{id}", async (
            Guid id,
            IRoleService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.GetByIdAsync(id, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.NotFound(result);
        });

        // CREATE ROLE
        app.MapPost(string.Empty, async (
            RoleCreateDto request,
            IRoleService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.CreateAsync(request, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.Conflict(result.StatusCode);
        })
            .Produces<Result<string>>()
            .AddEndpointFilter<FluentValidationFilter<RoleCreateDto>>();

        // UPDATE ROLE
        app.MapPut(string.Empty, async (
            RoleUpdateDto request,
            IRoleService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.UpdateAsync(request, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.StatusCode(result.StatusCode);
        })
            .Produces<Result<string>>()
            .AddEndpointFilter<FluentValidationFilter<RoleUpdateDto>>();

        // DELETE ROLE
        app.MapDelete("{id}", async (
            Guid id,
            IRoleService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.DeleteAsync(id, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.NotFound(result);
        });

    }
}

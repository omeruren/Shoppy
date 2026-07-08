using Asp.Versioning;
using Carter;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Permissions;
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
            .RequireRateLimiting("fixed");

        // GET ALL ROLES
        app.MapGet(string.Empty, async (
            IRoleService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.GetAllAsync(cancellationToken);

            return result.ToHttpResult();
        }).RequireAuthorization(Permissions.Roles.Read);

        // GET ROLE BY ID
        app.MapGet("{id}", async (
            Guid id,
            IRoleService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.GetByIdAsync(id, cancellationToken);

            return result.ToHttpResult();
        }).RequireAuthorization(Permissions.Roles.Read);

        // CREATE ROLE
        app.MapPost(string.Empty, async (
            RoleCreateDto request,
            IRoleService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.CreateAsync(request, cancellationToken);

            return result.ToHttpResult(location: string.Empty);
        })
            .Produces<Result<string>>()
            .AddEndpointFilter<FluentValidationFilter<RoleCreateDto>>()
            .RequireAuthorization(Permissions.Roles.Create);

        // UPDATE ROLE
        app.MapPut("{id:guid}", async (
            Guid id,
            RoleUpdateDto request,
            IRoleService _service,
            CancellationToken cancellationToken) =>
        {
            request = request with { Id = id };
            var result = await _service.UpdateAsync(request, cancellationToken);

            return result.ToHttpResult();
        })
            .Produces<Result<string>>()
            .AddEndpointFilter<FluentValidationFilter<RoleUpdateDto>>()
            .RequireAuthorization(Permissions.Roles.Update);

        // DELETE ROLE
        app.MapDelete("{id}", async (
            Guid id,
            IRoleService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.DeleteAsync(id, cancellationToken);

            return result.ToHttpResult();
        }).RequireAuthorization(Permissions.Roles.Delete);

        // GET ROLE PERMISSIONS
        app.MapGet("{id:guid}/permissions", async (
            Guid id,
            IRoleService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.GetPermissionsAsync(id, cancellationToken);

            return result.ToHttpResult();
        }).RequireAuthorization(Permissions.Roles.Read);

        // UPDATE ROLE PERMISSIONS
        app.MapPut("{id:guid}/permissions", async (
            Guid id,
            UpdateRolePermissionsDto request,
            IRoleService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.UpdatePermissionsAsync(id, request.Permissions, cancellationToken);

            return result.ToHttpResult();
        })
            .AddEndpointFilter<FluentValidationFilter<UpdateRolePermissionsDto>>()
            .RequireAuthorization(Permissions.Roles.Update);

        // GET PERMISSION CATALOG (all permissions the app knows about, for building role-permission UIs)
        var permissionsApp = builder
            .MapGroup("/api/v{version:apiVersion}/permissions")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Roles")
            .RequireRateLimiting("fixed");

        permissionsApp.MapGet(string.Empty, () =>
        {
            var catalog = Permissions.GetAll()
                .Select(p => new PermissionCatalogDto(p, p[..p.IndexOf('.')]))
                .ToList();

            return Result<List<PermissionCatalogDto>>.Success(catalog).ToHttpResult();
        }).RequireAuthorization(Permissions.Roles.Read);

    }
}

using Asp.Versioning;
using Carter;
using Shoppy.Business.UserRoles;
using Shoppy.Business.UserRoles.DataTransferObjects;

namespace Shoppy.WebAPI.Modules;

public sealed class UserRoleModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder builder)
    {
        var apiVersionSet = builder.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var app = builder.MapGroup("/api/v{version:apiVersion}/user-roles")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("UserRoles")
            .RequireRateLimiting("fixed")
            .RequireAuthorization("Admin");

        // GET ALL USER ROLES
        app.MapGet(string.Empty, async (
            IUserRoleService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.GetAllAsync(cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.StatusCode(result.StatusCode);
        });

        // CREATE USER ROLE
        app.MapPost(string.Empty, async (
            UserRoleCreateDto request,
            IUserRoleService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.CreateAsync(request, cancellationToken);

            return result.IsSuccessful ? Results.Created(string.Empty, result) : Results.StatusCode(result.StatusCode);
        });


        // DELETE USER ROLE
        app.MapDelete("{id}", async (
            Guid id,
            IUserRoleService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.DeleteAsync(id, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.NotFound(result);
        });
    }
}
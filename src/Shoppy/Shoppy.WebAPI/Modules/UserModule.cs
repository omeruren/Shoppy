using Asp.Versioning;
using Carter;
using Shoppy.Business.Permissions;
using Shoppy.Business.Users;
using Shoppy.Business.Users.DataTransferObjects;
using Shoppy.Business.Extensions;
using System.Security.Claims;

namespace Shoppy.WebAPI.Modules;

public class UserModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder builder)
    {
        var apiVersionSet = builder.NewApiVersionSet()
              .HasApiVersion(new ApiVersion(1, 0))
              .ReportApiVersions()
              .Build();

        var app = builder.MapGroup("/api/v{version:apiVersion}/users")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Users")
            .RequireRateLimiting("fixed");

        // GET ALL USERS

        app.MapGet(string.Empty, async (
            IUserService _service,
            int pageNumber = 1,
            int pageSize = 5,
            string searchTerm = "",
            CancellationToken cancellationToken = default) =>
        {
            var paginationRequest = new PaginationRequestDto(pageNumber, pageSize, searchTerm);
            var result = await _service.GetAllAsync(paginationRequest, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.StatusCode(result.StatusCode);
        }).RequireAuthorization(Permissions.Users.Read);


        // GET USER BY ID

        app.MapGet("{id}", async (
            Guid id,
            IUserService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.GetByIdAsync(id, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.NotFound(result);
        }).RequireAuthorization(Permissions.Users.Read);


        // CREATE USER

        app.MapPost(string.Empty, async (
            UserCreateDto request,
            IUserService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.CreateAsync(request, cancellationToken);

            return result.IsSuccessful ? Results.Created(string.Empty, result) : Results.Conflict(result);
        }).RequireAuthorization(Permissions.Users.Create);

        // UPDATE USER

        app.MapPut(string.Empty, async (
            UserUpdateDto request,
            IUserService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.UpdateAsync(request, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.StatusCode(result.StatusCode);
        }).RequireAuthorization(Permissions.Users.Update);


        // DELETE USER

        app.MapDelete("{id}", async (
            Guid id,
            IUserService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.DeleteAsync(id, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.NotFound(result);
        }).RequireAuthorization(Permissions.Users.Delete);


        // ── Self-service: current user's own account ──────────────────────

        // GET MY PROFILE

        app.MapGet("me", async (
            ClaimsPrincipal user,
            IUserService _service,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _service.GetProfileAsync(userId, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.NotFound(result);
        }).RequireAuthorization();

        // UPDATE MY PROFILE

        app.MapPut("me", async (
            ClaimsPrincipal user,
            UserUpdateSelfDto request,
            IUserService _service,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _service.UpdateSelfAsync(userId, request, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.StatusCode(result.StatusCode);
        }).RequireAuthorization();

        // CHANGE MY PASSWORD

        app.MapPost("me/change-password", async (
            ClaimsPrincipal user,
            ChangePasswordDto request,
            IUserService _service,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _service.ChangePasswordAsync(userId, request, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.StatusCode(result.StatusCode);
        }).RequireAuthorization();

    }
}

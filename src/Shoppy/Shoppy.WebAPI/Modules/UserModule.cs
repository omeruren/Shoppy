using Asp.Versioning;
using Carter;
using Shoppy.Business.Extensions;
using Shoppy.Business.Users;
using Shoppy.Business.Users.DataTransferObjects;

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
        }).RequireRateLimiting("Admin");


        // GET USER BY ID

        app.MapGet("{id}", async (
            Guid id,
            IUserService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.GetByIdAsync(id, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.NotFound(result);
        }).RequireRateLimiting("Admin");


        // CREATE USER

        app.MapPost(string.Empty, async (
            UserCreateDto request,
            IUserService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.CreateAsync(request, cancellationToken);

            return result.IsSuccessful ? Results.Created(string.Empty, result) : Results.Conflict(result);
        }).RequireRateLimiting("Admin");

        // UPDATE USER

        app.MapPut(string.Empty, async (
            UserUpdateDto request,
            IUserService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.UpdateAsync(request, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.StatusCode(result.StatusCode);
        }).RequireRateLimiting("Admin");


        // DELETE USER

        app.MapDelete("{id}", async (
            Guid id,
            IUserService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.DeleteAsync(id, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.NotFound(result);
        }).RequireRateLimiting("Admin");

    }
}

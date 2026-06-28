using Carter;
using Shoppy.Business.Users;
using Shoppy.Business.Users.DataTransferObjects;

namespace Shoppy.WebAPI.Modules;

public class UserModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder builder)
    {
        var app = builder.MapGroup("/users").WithTags("Users");



        // GET ALL USERS

        app.MapGet(string.Empty, async (
            IUserService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.GetAllAsync(cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.StatusCode(result.StatusCode);
        });


        // GET USER BY ID

        app.MapGet("{id}", async (
            Guid id,
            IUserService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.GetByIdAsync(id, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.NotFound(result);
        });


        // CREATE USER

        app.MapPost(string.Empty, async (
            UserCreateDto request,
            IUserService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.CreateAsync(request, cancellationToken);

            return result.IsSuccessful ? Results.Created(string.Empty, result) : Results.Conflict(result);
        });

        // UPDATE USER

        app.MapPut(string.Empty, async (
            UserUpdateDto request,
            IUserService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.UpdateAsync(request, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.StatusCode(result.StatusCode);
        });


        // DELETE USER

        app.MapDelete("{id}", async (
            Guid id,
            IUserService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.DeleteAsync(id, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.NotFound(result);
        });

    }
}

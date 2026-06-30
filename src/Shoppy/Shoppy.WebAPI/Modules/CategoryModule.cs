using Carter;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Categories;
using Shoppy.Business.Categories.DataTransferObjects;
using Shoppy.WebAPI.Filters;

namespace Shoppy.WebAPI.Modules;

public sealed class CategoryModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder builder)
    {
        var app = builder.MapGroup("/categories").WithTags("Categories").RequireAuthorization();

        // GET ALL CATEGORIES

        app.MapGet(string.Empty, async (
            ICategoryService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.GetallAsync(cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.StatusCode(result.StatusCode);

        }).Produces<Result<List<CategoryResultDto>>>();


        // GET CATEGORY BY ID

        app.MapGet("{id}", async (
            Guid id,
            ICategoryService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.GetByIdAsync(id, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.NotFound(result);

        }).Produces<Result<CategoryResultDto>>();

        // CREATE CATEGORY

        app.MapPost(string.Empty, async (
            CategoryCreateDto request,
            ICategoryService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.CreateAsync(request, cancellationToken);

            return result.IsSuccessful ? Results.Created(string.Empty, result) : Results.Conflict(result.StatusCode);

        })
            .Produces<Result<string>>()
            .AddEndpointFilter<FluentValidationFilter<CategoryCreateDto>>();

        // UPDATE CATEGORY

        app.MapPut(string.Empty, async (
            CategoryUpdateDto request,
            ICategoryService _service,
          CancellationToken cancellationToken) =>
        {
            var result = await _service.UpdateAsync(request, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.StatusCode(result.StatusCode);

        })
            .Produces<Result<string>>()
            .AddEndpointFilter<FluentValidationFilter<CategoryUpdateDto>>();

        // DELETE CATEGORY

        app.MapDelete("{id}", async (
            Guid id,
            ICategoryService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.DeleteAsync(id, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.NotFound(result);

        }).Produces<Result<string>>();
    }
}
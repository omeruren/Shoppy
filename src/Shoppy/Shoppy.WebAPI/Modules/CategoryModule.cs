using Asp.Versioning;
using Carter;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Categories;
using Shoppy.Business.Categories.DataTransferObjects;
using Shoppy.Business.Extensions;
using Shoppy.Business.Permissions;
using Shoppy.WebAPI.Filters;

namespace Shoppy.WebAPI.Modules;

public sealed class CategoryModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder builder)
    {
        var apiVersionSet = builder.NewApiVersionSet()
           .HasApiVersion(new ApiVersion(1, 0))
           .ReportApiVersions()
           .Build();

        var app = builder
            .MapGroup("/api/v{version:apiVersion}/categories")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Categories")
            .RequireRateLimiting("fixed");

        // GET ALL CATEGORIES

        app.MapGet(string.Empty, async (
            ICategoryService _service,
            int pageNumber = 1,
            int pagesize = 5,
            string searchTerm = "",
            CancellationToken cancellationToken = default) =>
        {
            var paginationRequest = new PaginationRequestDto(pageNumber, pagesize, searchTerm);

            var result = await _service.GetallAsync(paginationRequest, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.StatusCode(result.StatusCode);

        })
            .Produces<Result<List<CategoryResultDto>>>()
            .RequireAuthorization(Permissions.Categories.Read);


        // GET CATEGORY BY ID

        app.MapGet("{id}", async (
            Guid id,
            ICategoryService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.GetByIdAsync(id, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.NotFound(result);

        })
            .Produces<Result<CategoryResultDto>>()
            .RequireAuthorization(Permissions.Categories.Read);

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
            .AddEndpointFilter<FluentValidationFilter<CategoryCreateDto>>()
            .RequireAuthorization(Permissions.Categories.Create);

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
            .AddEndpointFilter<FluentValidationFilter<CategoryUpdateDto>>()
            .RequireAuthorization(Permissions.Categories.Update);

        // DELETE CATEGORY

        app.MapDelete("{id}", async (
            Guid id,
            ICategoryService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.DeleteAsync(id, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.NotFound(result);

        })
            .Produces<Result<string>>()
            .RequireAuthorization(Permissions.Categories.Delete);
    }
}
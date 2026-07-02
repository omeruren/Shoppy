using Asp.Versioning;
using Carter;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Extensions;
using Shoppy.Business.Permissions;
using Shoppy.Business.Products;
using Shoppy.Business.Products.DataTransferObjects;
using Shoppy.WebAPI.Filters;

namespace Shoppy.WebAPI.Modules;

public class ProductModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder builder)
    {
        var apiVersionSet = builder.NewApiVersionSet()
          .HasApiVersion(new ApiVersion(1, 0))
          .ReportApiVersions()
          .Build();

        var app = builder
            .MapGroup("/api/v{version:apiVersion}/products")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Products")
            .RequireRateLimiting("fixed");

        // GET ALL PRODUCTS

        app.MapGet(string.Empty, async (
            IProductService _service,
            int pageNumber = 1,
            int pageSize = 5,
            string searchTerm = "",
            CancellationToken cancelllationToken = default) =>
        {
            var paginationRequest = new PaginationRequestDto(pageNumber, pageSize, searchTerm);
            var result = await _service.GetAllAsync(paginationRequest, cancelllationToken);


            return result.IsSuccessful ? Results.Ok(result) : Results.StatusCode(result.StatusCode);

        })
            .Produces<Result<List<ProductResultDto>>>()
            .RequireAuthorization(Permissions.Products.Read);

        // GET PRODUCT BY ID

        app.MapGet("{id}", async (
            IProductService _service,
            Guid id,
            CancellationToken cancelllationToken) =>
        {
            var result = await _service.GetByIdAsync(id, cancelllationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.NotFound(result);

        })
            .Produces<Result<ProductResultDto>>()
            .RequireAuthorization(Permissions.Products.Read);

        // CREATE PRODUCT

        app.MapPost(string.Empty, async (
            ProductCreateDto request,
            IProductService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.CreateAsync(request, cancellationToken);

            return result.IsSuccessful ? Results.Created(string.Empty, result) : Results.Conflict(result.StatusCode);

        })
            .Produces<Result<string>>()
            .AddEndpointFilter<FluentValidationFilter<ProductCreateDto>>()
            .RequireAuthorization(Permissions.Products.Create);

        // UPDATE PRODUCT

        app.MapPut(string.Empty, async (
            ProductUpdateDto request,
            IProductService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.UpdateAsync(request, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.StatusCode(result.StatusCode);

        })
            .Produces<Result<string>>()
            .AddEndpointFilter<FluentValidationFilter<ProductUpdateDto>>()
            .RequireAuthorization(Permissions.Products.Update);

        // DELETE PRODUCT

        app.MapDelete("{id}", async (
            Guid id,
            IProductService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.DeleteAsync(id, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.NotFound(result);

        })
            .Produces<Result<string>>()
            .RequireAuthorization(Permissions.Products.Delete);
    }
}

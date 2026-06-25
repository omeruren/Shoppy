using Carter;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Products;
using Shoppy.Business.Products.DataTransferObjects;
using Shoppy.WebAPI.Filters;

namespace Shoppy.WebAPI.Modules;

public class ProductModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder builder)
    {
        var app = builder.MapGroup("/products").WithTags("Products");


        // GET ALL PRODUCTS

        app.MapGet(string.Empty, async (
            IProductService _service,
            CancellationToken cancelllationToken) =>
        {
            var result = await _service.GetAllAsync(cancelllationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.StatusCode(result.StatusCode);

        }).Produces<Result<List<ProductResultDto>>>();

        // GET PRODUCT BY ID

        app.MapGet("{id}", async (
            IProductService _service,
            Guid id,
            CancellationToken cancelllationToken) =>
        {
            var result = await _service.GetByIdAsync(id, cancelllationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.NotFound(result);

        }).Produces<Result<ProductResultDto>>();

        // CREATE PRODUCT

        app.MapPost(string.Empty, async (
            ProductCreateDto request,
            IProductService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.CreateAsync(request, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.Conflict(result.StatusCode);

        })
            .Produces<Result<string>>()
            .AddEndpointFilter<FluentValidationFilter<ProductCreateDto>>();

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
            .AddEndpointFilter<FluentValidationFilter<ProductUpdateDto>>();

        // DELETE PRODUCT

        app.MapDelete("{id}", async (
            Guid id,
            IProductService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.DeleteAsync(id, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.NotFound(result);

        }).Produces<Result<string>>();
    }
}

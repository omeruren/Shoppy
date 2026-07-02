using Asp.Versioning;
using Carter;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Extensions;
using Shoppy.Business.Orders;
using Shoppy.Business.Orders.DataTransferObjects;
using Shoppy.WebAPI.Filters;

namespace Shoppy.WebAPI.Modules;

public class OrderModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder builder)
    {
        var apiVersionSet = builder.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var app = builder
            .MapGroup("/api/v{version:apiVersion}/orders")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Orders")
            .RequireRateLimiting("fixed")
            .RequireAuthorization();

        // GET ALL ORDERS

        app.MapGet(string.Empty, async (
            IOrderService _service,
            int pageNumber = 1,
            int pageSize = 10,
            string searchTerm = "",
            CancellationToken cancelllationToken = default) =>
        {
            var paginationRequest = new PaginationRequestDto(pageNumber, pageSize, searchTerm);

            var result = await _service.GetAllAsync(paginationRequest, cancelllationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.StatusCode(result.StatusCode);

        }).Produces<Result<List<OrderResultDto>>>();

        // GET ORDER BY ID

        app.MapGet("{id}", async (
            IOrderService _service,
            Guid id,
            CancellationToken cancelllationToken) =>
        {
            var result = await _service.GetByIdAsync(id, cancelllationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.NotFound(result);

        }).Produces<Result<OrderResultDto>>();

        // CREATE ORDER

        app.MapPost(string.Empty, async (
            OrderCreateDto request,
            IOrderService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.CreateAsync(request, cancellationToken);

            return result.IsSuccessful ? Results.Created(string.Empty, result) : Results.Conflict(result.StatusCode);

        })
            .Produces<Result<string>>()
            .AddEndpointFilter<FluentValidationFilter<OrderCreateDto>>();

        // UPDATE ORDER

        app.MapPut(string.Empty, async (
            OrderUpdateDto request,
            IOrderService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.UpdateAsync(request, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.StatusCode(result.StatusCode);

        })
            .Produces<Result<string>>()
            .AddEndpointFilter<FluentValidationFilter<OrderUpdateDto>>();

        // DELETE ORDER

        app.MapDelete("{id}", async (
            Guid id,
            IOrderService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.DeleteAsync(id, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.NotFound(result);

        }).Produces<Result<string>>();
    }
}


using Carter;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Extensions;
using Shoppy.Business.OrderItems;
using Shoppy.Business.OrderItems.DataTransferObjects;
using Shoppy.WebAPI.Filters;

namespace Shoppy.WebAPI.Modules;

public sealed class OrderItemModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder builder)
    {
        var app = builder
            .MapGroup("/items")
            .WithTags("Items")
            .RequireRateLimiting("fixed");

        // GET ALL ITEMS

        app.MapGet(string.Empty, async (
            IOrderItemService _service,
            int pageNumber = 1,
            int pageSize = 5,
            string searchTerm = "",
            CancellationToken cancellationToken = default) =>
        {
            var paginationRequest = new PaginationRequestDto(pageNumber, pageSize, searchTerm);

            var result = await _service.GetAllAsync(paginationRequest, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.StatusCode(result.StatusCode);

        }).Produces<Result<List<OrderItemResultDto>>>();


        // GET ITEM BY ID

        app.MapGet("{id}", async (
            Guid id,
            IOrderItemService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.GetByIdAsync(id, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.NotFound(result);

        }).Produces<Result<OrderItemResultDto>>();

        // CREATE ITEM

        app.MapPost(string.Empty, async (
            OrderItemCreateDto request,
            IOrderItemService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.CreateAsync(request, cancellationToken);

            return result.IsSuccessful ? Results.Created(string.Empty, result) : Results.Conflict(result.StatusCode);

        })
            .Produces<Result<string>>()
            .AddEndpointFilter<FluentValidationFilter<OrderItemCreateDto>>();

        // UPDATE ITEM

        app.MapPut(string.Empty, async (
            OrderItemUpdateDto request,
            IOrderItemService _service,
          CancellationToken cancellationToken) =>
        {
            var result = await _service.UpdateAsync(request, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.StatusCode(result.StatusCode);

        })
            .Produces<Result<string>>()
            .AddEndpointFilter<FluentValidationFilter<OrderItemUpdateDto>>();

        // DELETE ITEM

        app.MapDelete("{id}", async (
            Guid id,
            IOrderItemService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.DeleteAsync(id, cancellationToken);

            return result.IsSuccessful ? Results.Ok(result) : Results.NotFound(result);

        }).Produces<Result<string>>();
    }
}

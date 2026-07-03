using Asp.Versioning;
using Carter;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Extensions;
using Shoppy.Business.OrderItems;
using Shoppy.Business.OrderItems.DataTransferObjects;
using Shoppy.Business.Permissions;
using Shoppy.WebAPI.Filters;

namespace Shoppy.WebAPI.Modules;

public sealed class OrderItemModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder builder)
    {
        var apiVersionSet = builder.NewApiVersionSet()
           .HasApiVersion(new ApiVersion(1, 0))
           .ReportApiVersions()
           .Build();

        var app = builder
            .MapGroup("/api/v{version:apiVersion}/order-items")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("OrderItems")
            .RequireRateLimiting("fixed");

        // GET ALL ITEMS

        app.MapGet(string.Empty, async (
            IOrderItemService _service,
            int pageNumber = 1,
            int pageSize = 5,
            string searchTerm = "",
            string? sortBy = null,
            string? sortDirection = null,
            CancellationToken cancellationToken = default) =>
        {
            var paginationRequest = new PaginationRequestDto(pageNumber, pageSize, searchTerm, sortBy, sortDirection);

            var result = await _service.GetAllAsync(paginationRequest, cancellationToken);

            return result.ToHttpResult();

        })
            .Produces<Result<List<OrderItemResultDto>>>()
            .RequireAuthorization(Permissions.OrderItems.Read);


        // GET ITEM BY ID

        app.MapGet("{id}", async (
            Guid id,
            IOrderItemService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.GetByIdAsync(id, cancellationToken);

            return result.ToHttpResult();

        })
            .Produces<Result<OrderItemResultDto>>()
            .RequireAuthorization(Permissions.OrderItems.Read);

        // CREATE ITEM

        app.MapPost(string.Empty, async (
            OrderItemCreateDto request,
            IOrderItemService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.CreateAsync(request, cancellationToken);

            return result.ToHttpResult(location: string.Empty);

        })
            .Produces<Result<string>>()
            .AddEndpointFilter<FluentValidationFilter<OrderItemCreateDto>>()
            .RequireAuthorization(Permissions.OrderItems.Create);

        // UPDATE ITEM

        app.MapPut("{id:guid}", async (
            Guid id,
            OrderItemUpdateDto request,
            IOrderItemService _service,
          CancellationToken cancellationToken) =>
        {
            request = request with { Id = id };
            var result = await _service.UpdateAsync(request, cancellationToken);

            return result.ToHttpResult();

        })
            .Produces<Result<string>>()
            .AddEndpointFilter<FluentValidationFilter<OrderItemUpdateDto>>()
            .RequireAuthorization(Permissions.OrderItems.Update);

        // DELETE ITEM

        app.MapDelete("{id}", async (
            Guid id,
            IOrderItemService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.DeleteAsync(id, cancellationToken);

            return result.ToHttpResult();

        })
            .Produces<Result<string>>()
            .RequireAuthorization(Permissions.OrderItems.Delete);
    }
}

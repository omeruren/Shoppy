using Asp.Versioning;
using Carter;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Extensions;
using Shoppy.Business.Orders;
using Shoppy.Business.Orders.DataTransferObjects;
using Shoppy.Business.Permissions;
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
            .RequireRateLimiting("fixed");

        // GET ALL ORDERS

        app.MapGet(string.Empty, async (
            IOrderService _service,
            int pageNumber = 1,
            int pageSize = 10,
            string searchTerm = "",
            string? sortBy = null,
            string? sortDirection = null,
            CancellationToken cancellationToken = default) =>
        {
            var paginationRequest = new PaginationRequestDto(pageNumber, pageSize, searchTerm, sortBy, sortDirection);

            var result = await _service.GetAllAsync(paginationRequest, cancellationToken);

            return result.ToHttpResult();

        })
            .Produces<Result<List<OrderResultDto>>>()
            .RequireAuthorization(Permissions.Orders.Read);

        // GET ORDER BY ID

        app.MapGet("{id}", async (
            IOrderService _service,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.GetByIdAsync(id, cancellationToken);

            return result.ToHttpResult();

        })
            .Produces<Result<OrderResultDto>>()
            .RequireAuthorization(Permissions.Orders.Read);

        // CREATE ORDER

        app.MapPost(string.Empty, async (
            OrderCreateDto request,
            IOrderService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.CreateAsync(request, cancellationToken);

            return result.ToHttpResult(location: string.Empty);

        })
            .Produces<Result<string>>()
            .AddEndpointFilter<FluentValidationFilter<OrderCreateDto>>()
            .RequireAuthorization(Permissions.Orders.Create);

        // UPDATE ORDER

        app.MapPut("{id:guid}", async (
            Guid id,
            OrderUpdateDto request,
            IOrderService _service,
            CancellationToken cancellationToken) =>
        {
            request = request with { Id = id };
            var result = await _service.UpdateAsync(request, cancellationToken);

            return result.ToHttpResult();

        })
            .Produces<Result<string>>()
            .AddEndpointFilter<FluentValidationFilter<OrderUpdateDto>>()
            .RequireAuthorization(Permissions.Orders.Update);

        // DELETE ORDER

        app.MapDelete("{id}", async (
            Guid id,
            IOrderService _service,
            CancellationToken cancellationToken) =>
        {
            var result = await _service.DeleteAsync(id, cancellationToken);

            return result.ToHttpResult();

        })
            .Produces<Result<string>>()
            .RequireAuthorization(Permissions.Orders.Delete);
    }
}


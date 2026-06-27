using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shoppy.Business.Categories;
using Shoppy.Business.OrderItems;
using Shoppy.Business.Orders;
using Shoppy.Business.Products;

namespace Shoppy.Business;

public static class BusinessRegistrar
{
    public static IServiceCollection AddBusiness(this IServiceCollection services)
    {
        // Service registrations
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOrderItemService, OrderItemService>();

        // Fluent Validation
        services.AddValidatorsFromAssembly(typeof(BusinessRegistrar).Assembly);

        return services;
    }
}

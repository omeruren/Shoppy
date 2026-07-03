using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shoppy.Business.Auth;
using Shoppy.Business.Caching;
using Shoppy.Business.Categories;
using Shoppy.Business.OrderItems;
using Shoppy.Business.Orders;
using Shoppy.Business.Products;
using Shoppy.Business.Roles;
using Shoppy.Business.Services;
using Shoppy.Business.UserRoles;
using Shoppy.Business.Users;

namespace Shoppy.Business;

public static class BusinessRegistrar
{
    public static IServiceCollection AddBusiness(this IServiceCollection services, IConfiguration configuration)
    {
        // HybridCache — L1 in-process + Redis as L2 distributed cache
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });
        services.AddHybridCache();
        services.AddSingleton<ICacheService, CacheService>();

        // Service registrations
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOrderItemService, OrderItemService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUserRoleService, UserRoleService>();
        services.AddScoped<IEmailService, SmtpEmailService>();

        services.AddScoped<JwtProvider>();

        // Fluent Validation
        services.AddValidatorsFromAssembly(typeof(BusinessRegistrar).Assembly);

        return services;
    }
}

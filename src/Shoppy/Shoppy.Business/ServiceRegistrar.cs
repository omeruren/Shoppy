using Microsoft.Extensions.DependencyInjection;
using Shoppy.Business.Categories;

namespace Shoppy.Business;

public static class ServiceRegistrar
{
    public static IServiceCollection AddBusiness(this IServiceCollection services)
    {
        // Service registrations
        services.AddScoped<ICategoryService, CategoryService>();

        return services;
    }
}

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shoppy.Business.Categories;

namespace Shoppy.Business;

public static class BusinessRegistrar
{
    public static IServiceCollection AddBusiness(this IServiceCollection services)
    {
        // Service registrations
        services.AddScoped<ICategoryService, CategoryService>();

        // Fluent Validation
        services.AddValidatorsFromAssembly(typeof(BusinessRegistrar).Assembly);

        return services;
    }
}

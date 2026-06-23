using FluentValidation;

namespace Shoppy.WebAPI.Filters;

public class FluentValidationFilter<T> : IEndpointFilter
{

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();

        if (validator is null) return await next(context);

        var model = context.Arguments.OfType<T>().FirstOrDefault();

        if (model is null) return Results.BadRequest("Request body is missing.");

        var result = await validator.ValidateAsync(model);

        if (!result.IsValid)
        {
            var errors = result.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return new ValidationExceptionEx(errors);
        }
        return await next(context);
    }
}

public sealed class ValidationExceptionEx(IDictionary<string, string[]> errors) : Exception
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}

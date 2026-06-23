using Microsoft.AspNetCore.Diagnostics;
using Shoppy.WebAPI.Filters;

namespace Shoppy.WebAPI.Handlers;

public class ExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ValidationExceptionEx validationException)
        {
            httpContext.Response.StatusCode =
                StatusCodes.Status422UnprocessableEntity;

            await httpContext.Response.WriteAsJsonAsync(
                new
                {
                    errors = validationException.Errors
                },
                cancellationToken);

            return true;
        }

        httpContext.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            new
            {
                message = exception.Message
            },
            cancellationToken);

        return true;
    }
}
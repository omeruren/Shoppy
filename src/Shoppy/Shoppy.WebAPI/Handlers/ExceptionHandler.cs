using Microsoft.AspNetCore.Diagnostics;
using Shoppy.WebAPI.Filters;
using System.Net;

namespace Shoppy.WebAPI.Handlers;

public class ExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is ValidationExceptionEx validationExceptionEx)
        {
            var errors = validationExceptionEx.Errors.SelectMany(e => e.Value).ToList();

            httpContext.Response.StatusCode = (int)HttpStatusCode.UnprocessableContent;

            await httpContext.Response.WriteAsJsonAsync(errors);

            return true;
        }

        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(new { exception.Message, httpContext.Response.StatusCode }, cancellationToken);

        return true;
    }
}

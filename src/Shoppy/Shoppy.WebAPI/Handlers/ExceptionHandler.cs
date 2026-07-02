using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shoppy.WebAPI.Filters;

namespace Shoppy.WebAPI.Handlers;

public class ExceptionHandler(ILogger<ExceptionHandler> _logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // handle validation exceptions separately
        if (exception is ValidationExceptionEx validationExceptionEx)
        {

            _logger.LogWarning("Validation Failed: {@Errors}", validationExceptionEx.Errors);

            var validationProblem = new ValidationProblemDetails(
                validationExceptionEx.Errors.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value))
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Validation failed",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Instance = httpContext.Request.Path
            };

            httpContext.Response.StatusCode =
                StatusCodes.Status422UnprocessableEntity;

            await httpContext.Response.WriteAsJsonAsync(validationProblem, cancellationToken);

            return true;
        }


        // Determine status code based on exception type

        var (statusCode, title) = exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request"),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Concurrency Conflict"),
            OperationCanceledException => (499, "Client Closed Request"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        // log the exception with appropriate level
        if (statusCode >= 500)
            _logger.LogError(exception, "Unhandled exception occurred. TraceId: {TraceId}", httpContext.TraceIdentifier);
        else
            _logger.LogWarning(exception, "Handled exception occurred. StatusCode : {StatusCode}", statusCode);

        // Return FRC 7807 Problem Details

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            // only expose exception message for non-500 errors (Client errors)
            Type = statusCode switch
            {
                400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                403 => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                409 => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            },
            Instance = httpContext.Request.Path
        };

        // Add TraceId for correlation
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
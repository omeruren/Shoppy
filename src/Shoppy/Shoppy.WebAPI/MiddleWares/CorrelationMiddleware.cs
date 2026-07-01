using Serilog.Context;

namespace Shoppy.WebAPI.MiddleWares;

public class CorrelationMiddleware(RequestDelegate _next)
{
    private const string CorrelationHeader = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        // read from incoming request or generate new one
        if (!context.Request.Headers.TryGetValue(CorrelationHeader, out var correlationId) || string.IsNullOrWhiteSpace(correlationId))
            correlationId = Guid.NewGuid().ToString();

        // set on response header
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.TryAdd(CorrelationHeader, correlationId.ToString());
            return Task.CompletedTask;
        });

        // push to serilog LogContext so all logs in this request include CorrelationId
        using (LogContext.PushProperty("CorrelationId", correlationId.ToString()))
        {
            await _next(context);
        }

    }
}

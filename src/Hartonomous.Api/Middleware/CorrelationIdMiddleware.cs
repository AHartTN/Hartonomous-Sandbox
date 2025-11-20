using System.Diagnostics;

namespace Hartonomous.Api.Middleware;

/// <summary>
/// Middleware that adds correlation IDs to response headers for request tracking.
/// Uses Activity.Current for distributed tracing correlation.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get current activity (created by ASP.NET Core automatically)
        var activity = Activity.Current;

        // Add correlation headers when response starts
        context.Response.OnStarting(() =>
        {
            // X-Correlation-ID: Root activity ID for the entire distributed trace
            context.Response.Headers.Append("X-Correlation-ID",
                activity?.RootId ?? context.TraceIdentifier);

            // X-Request-ID: This specific request's trace identifier
            context.Response.Headers.Append("X-Request-ID",
                context.TraceIdentifier);

            return Task.CompletedTask;
        });

        await _next(context);
    }
}

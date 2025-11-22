using System.Diagnostics;
using Hartonomous.Core.Utilities;
using System.Text;

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
            // X-Correlation-ID: Root trace ID for the entire distributed trace (GUID format)
            string correlationId;
            if (activity != null && activity.TraceId != default)
            {
                // Use Activity TraceId if available (W3C format: 32 hex chars)
                correlationId = activity.TraceId.ToHexString();
            }
            else
            {
                // Fallback: Generate deterministic GUID from TraceIdentifier
                correlationId = GenerateGuidFromString(context.TraceIdentifier).ToString();
            }
            context.Response.Headers.Append("X-Correlation-ID", correlationId);

            // X-Request-ID: This specific request's identifier
            var requestId = activity?.SpanId.ToHexString() ?? context.TraceIdentifier;
            context.Response.Headers.Append("X-Request-ID", requestId);

            return Task.CompletedTask;
        });

        await _next(context);
    }

    /// <summary>
    /// Generates a deterministic GUID from a string using SHA256 hashing.
    /// This ensures the same string always produces the same GUID.
    /// </summary>
    private static Guid GenerateGuidFromString(string input)
    {
        return HashUtilities.ComputeDeterministicGuid(input);
    }
}

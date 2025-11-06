using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Hartonomous.Infrastructure.Middleware;

/// <summary>
/// Middleware that ensures W3C Trace Context propagation and adds correlation IDs to responses.
/// Integrates with .NET's built-in distributed tracing (System.Diagnostics.Activity).
/// </summary>
public class CorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationMiddleware> _logger;
    
    // W3C Trace Context headers (https://www.w3.org/TR/trace-context/)
    private const string TraceParentHeader = "traceparent";
    private const string TraceStateHeader = "tracestate";
    
    // Response headers for client correlation
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private const string RequestIdHeader = "X-Request-ID";

    public CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var activity = Activity.Current;

        if (activity != null)
        {
            // W3C TraceContext uses TraceId as correlation ID (globally unique 16-byte identifier)
            var correlationId = activity.TraceId.ToString();
            var requestId = activity.SpanId.ToString();

            // Add correlation headers to response for client tracking
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
                {
                    context.Response.Headers[CorrelationIdHeader] = correlationId;
                }
                
                if (!context.Response.Headers.ContainsKey(RequestIdHeader))
                {
                    context.Response.Headers[RequestIdHeader] = requestId;
                }

                return Task.CompletedTask;
            });

            // Add correlation IDs to logger scope (available in all log messages for this request)
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["RequestId"] = requestId,
                ["TraceId"] = activity.TraceId.ToString(),
                ["SpanId"] = activity.SpanId.ToString(),
                ["ParentSpanId"] = activity.ParentSpanId.ToString()
            }))
            {
                _logger.LogDebug(
                    "Request started with TraceId: {TraceId}, SpanId: {SpanId}, ParentSpanId: {ParentSpanId}",
                    activity.TraceId,
                    activity.SpanId,
                    activity.ParentSpanId);

                await _next(context);

                _logger.LogDebug(
                    "Request completed with status {StatusCode} (TraceId: {TraceId})",
                    context.Response.StatusCode,
                    activity.TraceId);
            }
        }
        else
        {
            // No Activity.Current means distributed tracing isn't properly configured
            _logger.LogWarning("No Activity.Current found for request {Method} {Path}", 
                context.Request.Method, 
                context.Request.Path);

            await _next(context);
        }
    }
}

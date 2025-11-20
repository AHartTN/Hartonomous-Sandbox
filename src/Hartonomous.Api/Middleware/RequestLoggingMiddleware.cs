using System.Diagnostics;

namespace Hartonomous.Api.Middleware;

/// <summary>
/// Middleware that logs all HTTP requests and responses with timing information.
/// Integrates with Application Insights for distributed tracing.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var traceId = context.TraceIdentifier;
        var method = context.Request.Method;
        var path = context.Request.Path;

        _logger.LogInformation(
            "Request: {Method} {Path} TraceId: {TraceId}",
            method, path, traceId);

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var statusCode = context.Response.StatusCode;
            var duration = sw.ElapsedMilliseconds;

            _logger.LogInformation(
                "Response: {StatusCode} {Method} {Path} Duration: {Duration}ms TraceId: {TraceId}",
                statusCode, method, path, duration, traceId);
        }
    }
}

using System.Diagnostics;

namespace Hartonomous.Infrastructure.ProblemDetails;

/// <summary>
/// Customizes RFC 7807 Problem Details responses with Hartonomous-specific enrichments.
/// Adds correlation IDs, instance URLs, and tenant information.
/// </summary>
public static class ProblemDetailsCustomization
{
    /// <summary>
    /// Applies Hartonomous-specific enrichments to Problem Details.
    /// Called automatically by ASP.NET Core's Problem Details middleware.
    /// </summary>
    public static void CustomizeProblemDetails(Microsoft.AspNetCore.Http.ProblemDetailsContext context)
    {
        var httpContext = context.HttpContext;
        var problemDetails = context.ProblemDetails;

        // Add W3C Trace Context correlation IDs (from L4.10 CorrelationMiddleware)
        var activity = Activity.Current;
        if (activity != null)
        {
            problemDetails.Extensions["traceId"] = activity.TraceId.ToString();
            problemDetails.Extensions["spanId"] = activity.SpanId.ToString();
        }

        // Add correlation ID from response headers if available
        if (httpContext.Response.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId.ToString();
        }

        // Add instance URL (unique identifier for this specific problem occurrence)
        problemDetails.Instance = $"{httpContext.Request.Path}{httpContext.Request.QueryString}";

        // Add tenant information if available (from claims)
        var tenantId = httpContext.User.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantId))
        {
            problemDetails.Extensions["tenantId"] = tenantId;
        }

        // Add environment name for debugging
        problemDetails.Extensions["environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";

        // Add node/server identifier (useful in multi-instance deployments)
        problemDetails.Extensions["nodeId"] = Environment.MachineName;

        // Add timestamp (ISO 8601 format)
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("o");

        // Enrich exception details in Development environment only
        var env = httpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Hosting.IHostEnvironment)) as Microsoft.Extensions.Hosting.IHostEnvironment;
        if (env?.IsDevelopment() == true)
        {
            // Exception details are already included by ExceptionHandlerMiddleware
            // We just add a flag to indicate development mode
            problemDetails.Extensions["isDevelopment"] = true;
        }
    }
}

/// <summary>
/// Extension methods for configuring Problem Details services.
/// </summary>
public static class ProblemDetailsServiceExtensions
{
    /// <summary>
    /// Adds Hartonomous-customized Problem Details services.
    /// Configures RFC 7807-compliant error responses with custom enrichments.
    /// </summary>
    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddHartonomousProblemDetails(this Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            // Apply custom enrichments to all Problem Details responses
            options.CustomizeProblemDetails = ProblemDetailsCustomization.CustomizeProblemDetails;
        });

        return services;
    }
}

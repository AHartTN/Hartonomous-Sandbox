using System;
using System.Collections.Generic;

namespace Hartonomous.Infrastructure.Compliance;

/// <summary>
/// Configuration options for PII sanitization and redaction.
/// </summary>
public class PiiSanitizationOptions
{
    /// <summary>
    /// HMAC key ID for HmacRedactor (used for Private data classification).
    /// Should be rotated periodically in production.
    /// </summary>
    public long HmacKeyId { get; set; } = 1;

    /// <summary>
    /// HMAC key (base64 encoded) for HmacRedactor.
    /// IMPORTANT: Store in Azure Key Vault or App Configuration in production.
    /// </summary>
    public string HmacKey { get; set; } = Convert.ToBase64String("hartonomous-default-hmac-key-change-in-production"u8.ToArray());

    /// <summary>
    /// Enable redaction for HTTP logging (headers, route parameters).
    /// Recommended: true for Production, false for Development (for debugging).
    /// </summary>
    public bool EnableHttpLoggingRedaction { get; set; } = true;

    /// <summary>
    /// Enable redaction for structured logging via ILogger.
    /// Recommended: true for all environments.
    /// </summary>
    public bool EnableLoggerRedaction { get; set; } = true;

    /// <summary>
    /// Enable redaction for Problem Details responses (error responses).
    /// Recommended: true for all environments to prevent PII leakage in errors.
    /// </summary>
    public bool EnableProblemDetailsRedaction { get; set; } = true;

    /// <summary>
    /// HTTP request headers that contain personal data and should be redacted.
    /// Default: Authorization, Cookie, X-API-Key, X-User-Email.
    /// </summary>
    public HashSet<string> PersonalDataHeaders { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "X-API-Key",
        "X-User-Email",
        "X-User-Id"
    };

    /// <summary>
    /// HTTP route parameters that contain personal data and should be redacted.
    /// Default: userId, email, tenantId.
    /// </summary>
    public HashSet<string> PersonalDataRouteParameters { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "userId",
        "email",
        "tenantId",
        "customerId",
        "accountId"
    };

    /// <summary>
    /// Paths to exclude from HTTP logging entirely (e.g., health checks).
    /// Default: /health, /metrics, /ready, /live.
    /// </summary>
    public HashSet<string> ExcludeHttpLoggingPaths { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/health/ready",
        "/health/live",
        "/metrics",
        "/swagger"
    };
}

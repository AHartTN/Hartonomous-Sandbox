using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Hartonomous.Api.Authorization;

/// <summary>
/// Authorization handler that enforces tenant isolation.
/// Validates that the requesting user's tenant matches the resource's tenant.
/// </summary>
public class TenantResourceAuthorizationHandler : AuthorizationHandler<TenantResourceRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TenantResourceAuthorizationHandler> _logger;

    public TenantResourceAuthorizationHandler(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<TenantResourceAuthorizationHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantResourceRequirement requirement)
    {
        // Admin users bypass tenant isolation
        if (context.User.IsInRole("Admin"))
        {
            _logger.LogDebug("Admin user bypassing tenant isolation check");
            context.Succeed(requirement);
            return;
        }

        // Extract user's tenant ID from claims
        var userTenantIdClaim = context.User.FindFirst("tenant_id") 
            ?? context.User.FindFirst("extension_TenantId")
            ?? context.User.FindFirst(ClaimTypes.GroupSid); // Fallback

        if (userTenantIdClaim == null || !int.TryParse(userTenantIdClaim.Value, out var userTenantId))
        {
            _logger.LogWarning("User has no valid tenant_id claim. User: {User}", context.User.Identity?.Name);
            context.Fail();
            return;
        }

        // Extract requested tenant ID from route or query
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("No HTTP context available for tenant validation");
            context.Fail();
            return;
        }

        var requestedTenantId = GetRequestedTenantId(httpContext);

        if (requestedTenantId == null)
        {
            // No tenant ID in request - allow (some endpoints are tenant-agnostic)
            _logger.LogDebug("No tenant ID in request, allowing access");
            context.Succeed(requirement);
            return;
        }

        // Validate tenant match
        if (userTenantId != requestedTenantId.Value)
        {
            _logger.LogWarning(
                "Tenant mismatch: User tenant {UserTenantId} attempted to access resource for tenant {RequestedTenantId}. User: {User}",
                userTenantId, requestedTenantId.Value, context.User.Identity?.Name);
            context.Fail();
            return;
        }

        // For resource-specific checks, validate resource ownership
        if (!string.IsNullOrEmpty(requirement.ResourceType))
        {
            var resourceId = GetResourceId(httpContext);
            if (resourceId.HasValue)
            {
                var ownershipValid = await ValidateResourceOwnershipAsync(
                    requirement.ResourceType, 
                    resourceId.Value, 
                    userTenantId);

                if (!ownershipValid)
                {
                    _logger.LogWarning(
                        "Resource ownership validation failed: {ResourceType} {ResourceId} does not belong to tenant {TenantId}",
                        requirement.ResourceType, resourceId.Value, userTenantId);
                    context.Fail();
                    return;
                }
            }
        }

        _logger.LogDebug("Tenant isolation check passed for user {User}, tenant {TenantId}", 
            context.User.Identity?.Name, userTenantId);
        context.Succeed(requirement);
    }

    /// <summary>
    /// Extract requested tenant ID from route parameters, query string, or request body.
    /// </summary>
    private int? GetRequestedTenantId(HttpContext httpContext)
    {
        // Try route parameters first (e.g., /api/tenants/{tenantId}/...)
        if (httpContext.Request.RouteValues.TryGetValue("tenantId", out var routeTenantId))
        {
            if (int.TryParse(routeTenantId?.ToString(), out var tenantId))
                return tenantId;
        }

        // Try query string (e.g., ?tenantId=123)
        if (httpContext.Request.Query.TryGetValue("tenantId", out var queryTenantId))
        {
            if (int.TryParse(queryTenantId.ToString(), out var tenantId))
                return tenantId;
        }

        // For POST/PUT requests, could parse body (not implemented here for performance)
        // In production: cache parsed body or use custom model binder

        return null;
    }

    /// <summary>
    /// Extract resource ID from route parameters.
    /// </summary>
    private long? GetResourceId(HttpContext httpContext)
    {
        // Common patterns: atomId, embeddingId, jobId, etc.
        var idKeys = new[] { "atomId", "embeddingId", "jobId", "resourceId", "id" };

        foreach (var key in idKeys)
        {
            if (httpContext.Request.RouteValues.TryGetValue(key, out var value))
            {
                if (long.TryParse(value?.ToString(), out var resourceId))
                    return resourceId;
            }
        }

        return null;
    }

    /// <summary>
    /// Validate that a resource belongs to the specified tenant by querying the database.
    /// </summary>
    private async Task<bool> ValidateResourceOwnershipAsync(string resourceType, long resourceId, int tenantId)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("HartonomousDb");
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Database connection string not configured");
                return false;
            }

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Map resource type to table and ID column
            var (tableName, idColumn) = resourceType switch
            {
                "Atom" => ("dbo.Atoms", "AtomId"),
                "Embedding" => ("dbo.AtomEmbeddings", "AtomEmbeddingId"),
                "InferenceJob" => ("dbo.InferenceJobs", "JobId"),
                "Model" => ("dbo.Models", "ModelId"),
                _ => (null, null)
            };

            if (tableName == null || idColumn == null)
            {
                _logger.LogWarning("Unknown resource type: {ResourceType}", resourceType);
                return false; // Unknown resource type - deny by default
            }

            // Query to check if resource belongs to tenant
            var query = $@"
                SELECT COUNT(*)
                FROM {tableName}
                WHERE {idColumn} = @ResourceId
                  AND TenantId = @TenantId;
            ";

            await using var command = new SqlCommand(query, connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 5
            };

            command.Parameters.AddWithValue("@ResourceId", resourceId);
            command.Parameters.AddWithValue("@TenantId", tenantId);

            var result = await command.ExecuteScalarAsync();
            var count = result != null ? Convert.ToInt32(result) : 0;
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating resource ownership for {ResourceType} {ResourceId}", 
                resourceType, resourceId);
            return false; // Deny on error
        }
    }
}

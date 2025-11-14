using System;
using System.Linq;
using System.Security.Claims;
using Hartonomous.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Hartonomous.Infrastructure.Services.Tenant;

/// <summary>
/// Retrieves tenant context from HTTP request claims populated by Azure Entra External ID.
/// Uses TenantMappingService for safe GUID-to-INT resolution with database backing.
/// Falls back to HttpContext.Items cache for synchronous access.
///
/// IMPORTANT: Requires TenantResolutionMiddleware to run first to pre-resolve tenant GUID.
/// </summary>
public class HttpContextTenantContext : ITenantContext
{
    private const string TenantIdItemKey = "Hartonomous.TenantId";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public int? GetCurrentTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User == null)
            return null;

        // Check if tenant ID was already resolved by TenantResolutionMiddleware
        if (httpContext.Items.TryGetValue(TenantIdItemKey, out var cachedTenantId))
        {
            return cachedTenantId as int?;
        }

        // FALLBACK ONLY - Middleware should handle this
        // Try multiple claim types for tenant ID:
        // 1. "tenant_id" - custom claim from Entra External ID
        // 2. "tid" - standard Azure AD tenant ID claim
        // 3. "http://schemas.microsoft.com/identity/claims/tenantid" - legacy claim type
        var tenantClaim = httpContext.User.Claims.FirstOrDefault(c =>
            c.Type == "tenant_id" ||
            c.Type == "tid" ||
            c.Type == ClaimTypes.TenantId ||
            c.Type == "http://schemas.microsoft.com/identity/claims/tenantid");

        if (tenantClaim == null || string.IsNullOrWhiteSpace(tenantClaim.Value))
            return null;

        // Parse tenant ID - support both integer IDs and GUID-based IDs
        if (int.TryParse(tenantClaim.Value, out var tenantId))
        {
            httpContext.Items[TenantIdItemKey] = tenantId;
            return tenantId;
        }

        // CRITICAL: If GUID-based tenant ID found but not resolved by middleware,
        // this is a configuration error. Log warning and return null to avoid
        // unsafe GetHashCode() approach.
        if (Guid.TryParse(tenantClaim.Value, out var tenantGuid))
        {
            // TenantResolutionMiddleware should have resolved this already
            // Returning null forces proper error handling upstream
            return null;
        }

        return null;
    }

    public bool HasTenantContext => GetCurrentTenantId().HasValue;

    /// <summary>
    /// Internal method for TenantResolutionMiddleware to set resolved tenant ID.
    /// </summary>
    internal static void SetResolvedTenantId(HttpContext httpContext, int tenantId)
    {
        httpContext.Items[TenantIdItemKey] = tenantId;
    }
}

/// <summary>
/// Additional claim types for tenant identification.
/// </summary>
internal static class ClaimTypes
{
    /// <summary>
    /// Custom claim type for tenant ID in multi-tenant scenarios.
    /// </summary>
    public const string TenantId = "tenant_id";
}

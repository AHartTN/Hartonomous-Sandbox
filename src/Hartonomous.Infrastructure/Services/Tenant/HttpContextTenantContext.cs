using System;
using System.Linq;
using System.Security.Claims;
using Hartonomous.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Hartonomous.Infrastructure.Services.Tenant;

/// <summary>
/// Retrieves tenant context from HTTP request claims populated by Azure Entra External ID.
/// Supports both "tenant_id" and "tid" claim types for compatibility.
/// </summary>
public class HttpContextTenantContext : ITenantContext
{
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
        // For GUID-based tenant IDs from Azure AD, we'll need to map to our integer TenantId
        if (int.TryParse(tenantClaim.Value, out var tenantId))
            return tenantId;

        // If it's a GUID (Azure AD tenant ID), hash to integer for now
        // Production: Maintain a TenantMapping table to map Azure AD tenant GUIDs to internal integer IDs
        if (Guid.TryParse(tenantClaim.Value, out var tenantGuid))
        {
            // Use GetHashCode as deterministic mapping - stable across app restarts
            // This is acceptable for development, but production should use database mapping
            return Math.Abs(tenantGuid.GetHashCode()) % 1_000_000; // Keep in reasonable range
        }

        return null;
    }

    public bool HasTenantContext => GetCurrentTenantId().HasValue;
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

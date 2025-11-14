using System;
using System.Linq;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces;
using Hartonomous.Infrastructure.Services.Tenant;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Middleware;

/// <summary>
/// Resolves Azure Entra External ID tenant GUID to internal integer tenant ID at the start of each HTTP request.
/// Stores resolved tenant ID in HttpContext.Items for fast synchronous access by HttpContextTenantContext.
///
/// CRITICAL: Must run BEFORE any middleware/controllers that use ITenantContext.
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantMappingService tenantMappingService)
    {
        // Only process authenticated requests with user claims
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            // Extract tenant GUID from claims
            var tenantClaim = context.User.Claims.FirstOrDefault(c =>
                c.Type == "tenant_id" ||
                c.Type == "tid" ||
                c.Type == ClaimTypes.TenantId ||
                c.Type == "http://schemas.microsoft.com/identity/claims/tenantid");

            if (tenantClaim != null && !string.IsNullOrWhiteSpace(tenantClaim.Value))
            {
                // If already an integer, store directly
                if (int.TryParse(tenantClaim.Value, out var tenantId))
                {
                    HttpContextTenantContext.SetResolvedTenantId(context, tenantId);
                    _logger.LogDebug("Resolved integer tenant ID: {TenantId}", tenantId);
                }
                // If GUID, resolve via database
                else if (Guid.TryParse(tenantClaim.Value, out var tenantGuid))
                {
                    try
                    {
                        var resolvedTenantId = await tenantMappingService.ResolveTenantGuidAsync(
                            tenantGuid,
                            tenantName: null,
                            autoRegister: true,
                            cancellationToken: context.RequestAborted);

                        if (resolvedTenantId.HasValue)
                        {
                            HttpContextTenantContext.SetResolvedTenantId(context, resolvedTenantId.Value);
                            _logger.LogDebug(
                                "Resolved tenant GUID {TenantGuid} to tenant ID {TenantId}",
                                tenantGuid, resolvedTenantId.Value);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Failed to resolve tenant GUID {TenantGuid} (auto-registration may be disabled)",
                                tenantGuid);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Exception resolving tenant GUID {TenantGuid}: {ErrorMessage}",
                            tenantGuid, ex.Message);

                        // Don't block request - let it proceed without tenant context
                        // Downstream code should handle missing tenant appropriately
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "Tenant claim value '{TenantClaimValue}' is neither INT nor GUID",
                        tenantClaim.Value);
                }
            }
        }

        await _next(context);
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

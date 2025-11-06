using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hartonomous.Api.Authorization;

/// <summary>
/// Example controller demonstrating authorization handler usage.
/// Shows how to apply tenant isolation and role hierarchy policies.
/// </summary>
/// <remarks>
/// This is a documentation/example file. Delete or move to docs/ in production.
/// </remarks>
[ApiController]
[Route("api/examples")]
public class AuthorizationExamples : ControllerBase
{
    /// <summary>
    /// Example 1: Basic role hierarchy - any authenticated user.
    /// Admin, DataScientist, and User roles all have access.
    /// </summary>
    [HttpGet("public")]
    [Authorize(Policy = "RequireUser")]
    public IActionResult PublicEndpoint()
    {
        return Ok("Accessible by User, DataScientist, and Admin");
    }

    /// <summary>
    /// Example 2: DataScientist-level access.
    /// Only DataScientist and Admin roles have access (User denied).
    /// </summary>
    [HttpGet("analysis")]
    [Authorize(Policy = "RequireDataScientist")]
    public IActionResult AnalysisEndpoint()
    {
        return Ok("Accessible by DataScientist and Admin");
    }

    /// <summary>
    /// Example 3: Admin-only access.
    /// Only Admin role has access.
    /// </summary>
    [HttpPost("admin-action")]
    [Authorize(Policy = "RequireAdmin")]
    public IActionResult AdminEndpoint()
    {
        return Ok("Accessible by Admin only");
    }

    /// <summary>
    /// Example 4: Tenant isolation - user can only access their own tenant's data.
    /// Admins bypass this check automatically.
    /// </summary>
    [HttpGet("tenants/{tenantId}/data")]
    [Authorize(Policy = "TenantIsolation")]
    public IActionResult TenantDataEndpoint([FromRoute] int tenantId)
    {
        // TenantResourceAuthorizationHandler will validate:
        // 1. User's tenant_id claim matches the tenantId route parameter
        // 2. OR user has Admin role (bypasses check)
        return Ok($"Data for tenant {tenantId}");
    }

    /// <summary>
    /// Example 5: Resource-specific tenant isolation - accessing a specific Atom.
    /// Validates that the Atom belongs to the user's tenant.
    /// </summary>
    [HttpGet("atoms/{atomId}")]
    [Authorize(Policy = "TenantAtomAccess")]
    public IActionResult GetAtom([FromRoute] long atomId)
    {
        // TenantResourceAuthorizationHandler will:
        // 1. Extract user's tenant_id claim
        // 2. Query dbo.Atoms WHERE AtomId = @atomId AND TenantId = @userTenantId
        // 3. Deny if Atom doesn't belong to user's tenant
        // 4. Admins bypass this check
        return Ok($"Atom {atomId}");
    }

    /// <summary>
    /// Example 6: Combined policies - DataScientist role + tenant isolation.
    /// User must be DataScientist (or Admin) AND access their own tenant's embeddings.
    /// </summary>
    [HttpGet("tenants/{tenantId}/embeddings/{embeddingId}")]
    [Authorize(Policy = "RequireDataScientist")]
    [Authorize(Policy = "TenantEmbeddingAccess")]
    public IActionResult GetEmbedding([FromRoute] int tenantId, [FromRoute] long embeddingId)
    {
        // Both policies must succeed:
        // 1. RoleHierarchyHandler: User has DataScientist or Admin role
        // 2. TenantResourceAuthorizationHandler: Embedding belongs to user's tenant
        return Ok($"Embedding {embeddingId} for tenant {tenantId}");
    }

    /// <summary>
    /// Example 7: Legacy policy (backward compatible).
    /// Uses old RequireRole approach (still works).
    /// </summary>
    [HttpGet("legacy")]
    [Authorize(Policy = "Admin")]
    public IActionResult LegacyAdminEndpoint()
    {
        return Ok("Old-style Admin policy");
    }

    /// <summary>
    /// Example 8: Multiple authorization checks in code (for complex scenarios).
    /// </summary>
    [HttpPost("complex")]
    [Authorize(Policy = "RequireUser")]
    public async Task<IActionResult> ComplexAuthorizationAsync(
        [FromServices] IAuthorizationService authService,
        [FromBody] ComplexRequest request)
    {
        // Check if user has DataScientist role for this operation
        var roleAuth = await authService.AuthorizeAsync(
            User, 
            null, // No resource
            new RoleHierarchyRequirement("DataScientist"));

        if (!roleAuth.Succeeded)
        {
            return Forbid("DataScientist role required for this operation");
        }

        // Additional business logic here
        // (Tenant validation would be done via route parameter + TenantIsolation policy)

        return Ok("Complex authorization passed");
    }
}

public record ComplexRequest(int TenantId, string Operation);

/*
 * IMPLEMENTATION NOTES:
 * 
 * 1. **Tenant Claim Setup**:
 *    Azure AD token must include custom claim "tenant_id" or "extension_TenantId"
 *    Configure in Azure AD App Registration → Token Configuration → Add optional claim
 * 
 * 2. **Role Assignment**:
 *    Roles must be assigned in Azure AD:
 *    - Enterprise Applications → Users and groups → Assign roles
 *    - Roles appear in token as "roles" claim
 * 
 * 3. **Database Schema**:
 *    All tenant-scoped tables must have TenantId column:
 *    - dbo.Atoms (AtomId BIGINT, TenantId INT)
 *    - dbo.AtomEmbeddings (AtomEmbeddingId BIGINT, TenantId INT)
 *    - dbo.InferenceJobs (JobId BIGINT, TenantId INT)
 * 
 * 4. **Admin Bypass**:
 *    Users with Admin role bypass ALL tenant isolation checks
 *    Use carefully - admins have full cross-tenant access
 * 
 * 5. **Role Hierarchy**:
 *    Current hierarchy: Admin (100) > DataScientist (50) > User (10) > Anonymous (0)
 *    Modify RoleHierarchyHandler.RoleHierarchy dictionary to change
 * 
 * 6. **Performance**:
 *    TenantResourceAuthorizationHandler queries database for resource ownership
 *    Cache results in production or use claims-based approach
 *    Consider adding TenantId to claims after first validation
 * 
 * 7. **Testing**:
 *    Use ASP.NET Core TestServer with custom claims:
 *    ```csharp
 *    var claims = new[] {
 *        new Claim("tenant_id", "123"),
 *        new Claim(ClaimTypes.Role, "User")
 *    };
 *    var identity = new ClaimsIdentity(claims, "TestAuth");
 *    var user = new ClaimsPrincipal(identity);
 *    ```
 */

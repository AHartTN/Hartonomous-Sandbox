using Microsoft.AspNetCore.Authorization;

namespace Hartonomous.Api.Authorization;

/// <summary>
/// Authorization requirement for tenant-based resource access.
/// Ensures users can only access resources belonging to their tenant.
/// </summary>
public class TenantResourceRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Optional resource type being accessed (e.g., "Atom", "Embedding", "InferenceJob").
    /// </summary>
    public string? ResourceType { get; }

    /// <summary>
    /// Initializes a new tenant resource requirement.
    /// </summary>
    /// <param name="resourceType">Optional resource type for more granular authorization.</param>
    public TenantResourceRequirement(string? resourceType = null)
    {
        ResourceType = resourceType;
    }
}

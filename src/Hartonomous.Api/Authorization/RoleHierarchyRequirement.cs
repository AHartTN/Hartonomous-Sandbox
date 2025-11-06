using Microsoft.AspNetCore.Authorization;

namespace Hartonomous.Api.Authorization;

/// <summary>
/// Authorization requirement for role hierarchy.
/// Supports role inheritance where higher roles inherit permissions of lower roles.
/// Example: Admin > DataScientist > User
/// </summary>
public class RoleHierarchyRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Minimum required role to access the resource.
    /// </summary>
    public string MinimumRole { get; }

    /// <summary>
    /// Initializes a new role hierarchy requirement.
    /// </summary>
    /// <param name="minimumRole">The minimum role required (lower roles will be denied).</param>
    public RoleHierarchyRequirement(string minimumRole)
    {
        MinimumRole = minimumRole ?? throw new ArgumentNullException(nameof(minimumRole));
    }
}

using Microsoft.AspNetCore.Authorization;

namespace Hartonomous.Api.Authorization;

/// <summary>
/// Authorization handler that implements role hierarchy.
/// Higher roles automatically inherit permissions of lower roles.
/// Role hierarchy: Admin > DataScientist > User > Anonymous
/// </summary>
public class RoleHierarchyHandler : AuthorizationHandler<RoleHierarchyRequirement>
{
    private readonly ILogger<RoleHierarchyHandler> _logger;

    // Define role hierarchy from highest to lowest privilege
    private static readonly Dictionary<string, int> RoleHierarchy = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Admin", 100 },
        { "DataScientist", 50 },
        { "User", 10 },
        { "Anonymous", 0 }
    };

    public RoleHierarchyHandler(ILogger<RoleHierarchyHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleHierarchyRequirement requirement)
    {
        if (context.User.Identity == null || !context.User.Identity.IsAuthenticated)
        {
            _logger.LogDebug("User not authenticated, denying access for role requirement: {MinimumRole}", 
                requirement.MinimumRole);
            context.Fail();
            return Task.CompletedTask;
        }

        // Get the minimum required role level
        if (!RoleHierarchy.TryGetValue(requirement.MinimumRole, out var requiredLevel))
        {
            _logger.LogWarning("Unknown role in requirement: {MinimumRole}", requirement.MinimumRole);
            context.Fail();
            return Task.CompletedTask;
        }

        // Check if user has any role that meets or exceeds the requirement
        var userRoles = context.User.Claims
            .Where(c => c.Type == "roles" || c.Type == "role" || c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        if (!userRoles.Any())
        {
            _logger.LogDebug("User {User} has no roles assigned", context.User.Identity.Name);
            context.Fail();
            return Task.CompletedTask;
        }

        // Find the highest role level the user has
        var userHighestLevel = userRoles
            .Where(role => RoleHierarchy.ContainsKey(role))
            .Select(role => RoleHierarchy[role])
            .DefaultIfEmpty(0)
            .Max();

        if (userHighestLevel >= requiredLevel)
        {
            _logger.LogDebug(
                "User {User} granted access: highest role level {UserLevel} >= required level {RequiredLevel} ({MinimumRole})",
                context.User.Identity.Name, userHighestLevel, requiredLevel, requirement.MinimumRole);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "User {User} denied access: highest role level {UserLevel} < required level {RequiredLevel} ({MinimumRole}). User roles: {UserRoles}",
                context.User.Identity.Name, userHighestLevel, requiredLevel, requirement.MinimumRole, 
                string.Join(", ", userRoles));
            context.Fail();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks if a user has a role that meets or exceeds the minimum required role.
    /// </summary>
    /// <param name="userRole">The user's role.</param>
    /// <param name="minimumRole">The minimum required role.</param>
    /// <returns>True if the user's role is sufficient.</returns>
    public static bool MeetsRoleRequirement(string userRole, string minimumRole)
    {
        if (!RoleHierarchy.TryGetValue(userRole, out var userLevel))
            return false;

        if (!RoleHierarchy.TryGetValue(minimumRole, out var requiredLevel))
            return false;

        return userLevel >= requiredLevel;
    }

    /// <summary>
    /// Gets all roles that meet or exceed the minimum required role.
    /// </summary>
    /// <param name="minimumRole">The minimum required role.</param>
    /// <returns>List of roles that satisfy the requirement.</returns>
    public static IEnumerable<string> GetEligibleRoles(string minimumRole)
    {
        if (!RoleHierarchy.TryGetValue(minimumRole, out var requiredLevel))
            return Enumerable.Empty<string>();

        return RoleHierarchy
            .Where(kvp => kvp.Value >= requiredLevel)
            .Select(kvp => kvp.Key)
            .OrderByDescending(role => RoleHierarchy[role]);
    }
}

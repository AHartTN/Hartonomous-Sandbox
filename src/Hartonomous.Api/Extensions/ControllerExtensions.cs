using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hartonomous.Api.Extensions;

/// <summary>
/// Extension methods for ControllerBase to support composition over inheritance.
/// Provides common functionality without requiring a base controller class.
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// Gets the tenant ID from the current user's claims.
    /// </summary>
    /// <param name="controller">The controller instance</param>
    /// <returns>Tenant ID</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when tenant_id claim is not found</exception>
    public static int GetTenantId(this ControllerBase controller)
    {
        var claim = controller.User.FindFirst("tenant_id");
        if (claim == null)
            throw new UnauthorizedAccessException("Tenant ID not found in claims");

        return int.Parse(claim.Value);
    }

    /// <summary>
    /// Gets the user ID from the current user's claims.
    /// </summary>
    /// <param name="controller">The controller instance</param>
    /// <returns>User ID</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user ID claim is not found</exception>
    public static string GetUserId(this ControllerBase controller)
    {
        var claim = controller.User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null)
            throw new UnauthorizedAccessException("User ID not found in claims");

        return claim.Value;
    }

    /// <summary>
    /// Creates a paginated response with standard metadata.
    /// </summary>
    /// <typeparam name="T">Type of items in the response</typeparam>
    /// <param name="controller">The controller instance</param>
    /// <param name="items">Items for the current page</param>
    /// <param name="total">Total number of items across all pages</param>
    /// <param name="page">Current page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>OK result with paginated response</returns>
    public static ActionResult<PaginatedResponse<T>> Paginated<T>(
        this ControllerBase controller,
        IEnumerable<T> items,
        int total,
        int page,
        int pageSize)
    {
        return controller.Ok(new PaginatedResponse<T>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            HasNextPage = page < (int)Math.Ceiling(total / (double)pageSize),
            HasPreviousPage = page > 1
        });
    }
}

/// <summary>
/// Standard paginated response structure.
/// </summary>
/// <typeparam name="T">Type of items in the collection</typeparam>
public class PaginatedResponse<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

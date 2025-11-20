namespace Hartonomous.Core.DTOs;

/// <summary>
/// Standard API response wrapper for successful operations.
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The data returned by the operation.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Optional message providing additional context.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Timestamp of when the response was generated.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Standard error response with validation details.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Error type or category.
    /// </summary>
    public required string Error { get; set; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Trace identifier for request correlation.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Timestamp of when the error occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Field-level validation errors (if applicable).
    /// </summary>
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
}

/// <summary>
/// Paginated response for list operations.
/// </summary>
/// <typeparam name="T">The type of items in the collection</typeparam>
public class PaginatedResponse<T>
{
    /// <summary>
    /// Items for the current page.
    /// </summary>
    public required List<T> Items { get; set; }

    /// <summary>
    /// Current page number (1-indexed).
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

    /// <summary>
    /// Indicates whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Indicates whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}

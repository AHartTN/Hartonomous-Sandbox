using System.Collections.Generic;

namespace Hartonomous.Api.Extensions;

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

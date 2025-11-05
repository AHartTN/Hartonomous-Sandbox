using System;
using System.Collections.Generic;
using System.Linq;

namespace Hartonomous.Shared.Contracts.Results;

/// <summary>
/// Provides a consistent wrapper for paged responses.
/// </summary>
public sealed class PagedResult<T>
{
    public PagedResult(IEnumerable<T> items, int pageNumber, int pageSize, long totalCount)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), pageNumber, "Page number must be positive.");
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be positive.");
        }

        if (totalCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCount), totalCount, "Total count cannot be negative.");
        }

        Items = items?.ToArray() ?? Array.Empty<T>();
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items { get; }

    public int PageNumber { get; }

    public int PageSize { get; }

    public long TotalCount { get; }

    public long TotalPages => PageSize == 0 ? 0 : (long)Math.Ceiling((double)TotalCount / PageSize);

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;
}

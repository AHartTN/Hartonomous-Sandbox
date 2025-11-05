using System;

namespace Hartonomous.Shared.Contracts.Requests.Paging;

/// <summary>
/// Describes paging configuration requested by a client.
/// </summary>
public sealed class PagingOptions
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 500;

    public PagingOptions(int? pageNumber = null, int? pageSize = null)
    {
        PageNumber = NormalizePageNumber(pageNumber);
        PageSize = NormalizePageSize(pageSize);
    }

    public int PageNumber { get; }

    public int PageSize { get; }

    public static PagingOptions Create(int? pageNumber, int? pageSize)
        => new(pageNumber, pageSize);

    public PagingOptions WithOverrides(int? pageNumber = null, int? pageSize = null)
        => new(pageNumber ?? PageNumber, pageSize ?? PageSize);

    public static int NormalizePageNumber(int? pageNumber)
    {
        if (!pageNumber.HasValue)
        {
            return DefaultPageNumber;
        }

        if (pageNumber.Value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), pageNumber.Value, "Page number must be positive.");
        }

        return pageNumber.Value;
    }

    public static int NormalizePageSize(int? pageSize)
    {
        if (!pageSize.HasValue)
        {
            return DefaultPageSize;
        }

        if (pageSize.Value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize.Value, "Page size must be positive.");
        }

        return Math.Min(pageSize.Value, MaxPageSize);
    }
}

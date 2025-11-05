using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.Common;

public class PagedRequest
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 1000)]
    public int PageSize { get; set; } = 50;

    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
}

namespace Hartonomous.Api.Common;

/// <summary>
/// Standardized API response wrapper for all endpoints.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public ApiError? Error { get; set; }
    public ApiMetadata? Metadata { get; set; }

    public static ApiResponse<T> Ok(T data, ApiMetadata? metadata = null) => new()
    {
        Success = true,
        Data = data,
        Metadata = metadata
    };

    public static ApiResponse<T> Fail(string code, string message, object? details = null) => new()
    {
        Success = false,
        Error = new ApiError
        {
            Code = code,
            Message = message,
            Details = details
        }
    };
}

public class ApiError
{
    public required string Code { get; set; }
    public required string Message { get; set; }
    public object? Details { get; set; }
}

public class ApiMetadata
{
    public long? TotalCount { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public Dictionary<string, object>? Extra { get; set; }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Hartonomous.Shared.Contracts.Errors;
using Hartonomous.Shared.Contracts.Results;

namespace Hartonomous.Shared.Contracts.Responses;

/// <summary>
/// Canonical response envelope used by public APIs and internal services.
/// </summary>
public sealed class ApiResponse<T>
{
    private ApiResponse(bool success, T? data, IReadOnlyList<ErrorDetail> errors, string? correlationId, IReadOnlyDictionary<string, object?>? metadata)
    {
        Succeeded = success;
        Data = data;
        Errors = errors;
        CorrelationId = correlationId;
        Metadata = metadata ?? new Dictionary<string, object?>();
    }

    public bool Succeeded { get; }

    public T? Data { get; }

    public IReadOnlyList<ErrorDetail> Errors { get; }

    public string? CorrelationId { get; }

    public IReadOnlyDictionary<string, object?> Metadata { get; }

    public static ApiResponse<T> FromResult(OperationResult<T> result, IReadOnlyDictionary<string, object?>? metadata = null)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return result.Succeeded
            ? new(true, result.Value, Array.Empty<ErrorDetail>(), result.CorrelationId, metadata)
            : new(false, default, result.Errors, result.CorrelationId, metadata);
    }

    public static ApiResponse<T> Success(T data, string? correlationId = null, IReadOnlyDictionary<string, object?>? metadata = null)
        => new(true, data, Array.Empty<ErrorDetail>(), correlationId, metadata);

    public static ApiResponse<T> Failure(IEnumerable<ErrorDetail> errors, string? correlationId = null, IReadOnlyDictionary<string, object?>? metadata = null)
        => new(false, default, errors?.ToArray() ?? Array.Empty<ErrorDetail>(), correlationId, metadata);
}

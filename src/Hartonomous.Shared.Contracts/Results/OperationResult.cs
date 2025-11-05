using System;
using System.Collections.Generic;
using System.Linq;
using Hartonomous.Shared.Contracts.Errors;

namespace Hartonomous.Shared.Contracts.Results;

/// <summary>
/// Wraps the outcome of an operation with consistent success/error semantics.
/// </summary>
public sealed class OperationResult<T>
{
    private OperationResult(bool succeeded, T? value, IReadOnlyList<ErrorDetail>? errors, string? correlationId)
    {
        Succeeded = succeeded;
        Value = value;
        Errors = errors ?? Array.Empty<ErrorDetail>();
        CorrelationId = correlationId;
    }

    /// <summary>
    /// Indicates whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Correlation identifier propagated across services/clients.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Successful response payload (when <see cref="Succeeded"/> is true).
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Machine-readable error details.
    /// </summary>
    public IReadOnlyList<ErrorDetail> Errors { get; }

    public static OperationResult<T> Success(T value, string? correlationId = null)
        => new(true, value, null, correlationId);

    public static OperationResult<T> Failure(IEnumerable<ErrorDetail> errors, string? correlationId = null)
        => new(false, default, errors?.ToArray() ?? Array.Empty<ErrorDetail>(), correlationId);
}

public static class OperationResult
{
    public static OperationResult<T> Success<T>(T value, string? correlationId = null)
        => OperationResult<T>.Success(value, correlationId);

    public static OperationResult<T> Failure<T>(IEnumerable<ErrorDetail> errors, string? correlationId = null)
        => OperationResult<T>.Failure(errors, correlationId);
}

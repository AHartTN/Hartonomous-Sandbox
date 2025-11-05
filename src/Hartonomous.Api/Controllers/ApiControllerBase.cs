using Hartonomous.Shared.Contracts.Errors;
using Hartonomous.Shared.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// Provides helper methods for building canonical API responses.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected string CorrelationId => HttpContext.TraceIdentifier;

    protected ApiResponse<T> Success<T>(T data, IReadOnlyDictionary<string, object?>? metadata = null)
        => ApiResponse<T>.Success(data, CorrelationId, metadata);

    protected ApiResponse<T> Failure<T>(IEnumerable<ErrorDetail> errors, IReadOnlyDictionary<string, object?>? metadata = null)
        => ApiResponse<T>.Failure(errors, CorrelationId, metadata);

    protected ErrorDetail ValidationError(string message, string? target = null)
        => ErrorDetailFactory.Validation(message, target);

    protected ErrorDetail MissingField(string field)
        => ErrorDetailFactory.MissingField(field);
}

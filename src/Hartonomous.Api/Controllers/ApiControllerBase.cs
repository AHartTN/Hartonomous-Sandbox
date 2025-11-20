using Asp.Versioning;
using Hartonomous.Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// Base controller providing standardized response formatting and error handling.
/// All API controllers should inherit from this class for consistent behavior.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Logger instance for the controller.
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiControllerBase"/> class.
    /// </summary>
    /// <param name="logger">Logger instance</param>
    protected ApiControllerBase(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a successful response with data and optional message.
    /// </summary>
    /// <typeparam name="T">Type of data being returned</typeparam>
    /// <param name="data">Data to return</param>
    /// <param name="message">Optional success message</param>
    /// <returns>200 OK with standardized response</returns>
    protected IActionResult SuccessResult<T>(T data, string? message = null)
    {
        return Ok(new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Creates an error response with specified status code.
    /// </summary>
    /// <param name="error">Error message</param>
    /// <param name="statusCode">HTTP status code (default: 500)</param>
    /// <returns>Error response with specified status code</returns>
    protected IActionResult ErrorResult(string error, int statusCode = 500)
    {
        Logger.LogError("API Error: {Error}", error);
        return StatusCode(statusCode, new ErrorResponse
        {
            Error = statusCode switch
            {
                400 => "Bad Request",
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "Not Found",
                409 => "Conflict",
                _ => "Server Error"
            },
            Message = error,
            TraceId = HttpContext.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Creates a validation error response from ModelState.
    /// </summary>
    /// <param name="modelState">ModelState dictionary containing validation errors</param>
    /// <returns>400 Bad Request with validation details</returns>
    protected IActionResult ValidationError(ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                e => e.Key,
                e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray()
            );

        return BadRequest(new ErrorResponse
        {
            Error = "Validation Failed",
            Message = "One or more validation errors occurred",
            ValidationErrors = errors,
            TraceId = HttpContext.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Creates a paginated response for list operations.
    /// </summary>
    /// <typeparam name="T">Type of items in the collection</typeparam>
    /// <param name="items">Items for current page</param>
    /// <param name="page">Current page number (1-indexed)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="totalCount">Total number of items</param>
    /// <returns>200 OK with paginated response</returns>
    protected IActionResult PagedResult<T>(
        IEnumerable<T> items,
        int page,
        int pageSize,
        int totalCount)
    {
        return Ok(new PaginatedResponse<T>
        {
            Items = items.ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = totalCount
        });
    }

    /// <summary>
    /// Executes an async operation with standardized error handling.
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="operation">Async operation to execute</param>
    /// <param name="operationName">Name for logging</param>
    /// <returns>Success or error result</returns>
    protected async Task<IActionResult> ExecuteAsync<T>(
        Func<Task<T>> operation,
        string operationName)
    {
        try
        {
            var result = await operation();
            return SuccessResult(result);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "{Operation} - Invalid argument: {Message}", operationName, ex.Message);
            return ErrorResult(ex.Message, 400);
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogWarning(ex, "{Operation} - Invalid operation: {Message}", operationName, ex.Message);
            return ErrorResult(ex.Message, 409);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Operation} failed: {Message}", operationName, ex.Message);
            return ErrorResult("An unexpected error occurred. Please try again later.", 500);
        }
    }
}

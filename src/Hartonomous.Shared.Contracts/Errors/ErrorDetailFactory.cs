using System;
using System.Collections.Generic;

namespace Hartonomous.Shared.Contracts.Errors;

/// <summary>
/// Factory helpers for creating canonical <see cref="ErrorDetail"/> instances.
/// </summary>
public static class ErrorDetailFactory
{
    public static ErrorDetail Create(string code, string message, string? target = null, IReadOnlyDictionary<string, object?>? properties = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code cannot be null or whitespace.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be null or whitespace.", nameof(message));
        }

        return new ErrorDetail(code, message, target, properties ?? new Dictionary<string, object?>());
    }

    public static ErrorDetail Validation(string message, string? target = null, IReadOnlyDictionary<string, object?>? properties = null)
        => Create(ErrorCodes.Validation.InvalidRequest, message, target, properties);

    public static ErrorDetail MissingField(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new ArgumentException("Field name cannot be null or whitespace.", nameof(fieldName));
        }

        return Create(ErrorCodes.Validation.MissingRequiredField, $"The field '{fieldName}' is required.", fieldName);
    }

    public static ErrorDetail InvalidFieldValue(string fieldName, string reason)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new ArgumentException("Field name cannot be null or whitespace.", nameof(fieldName));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason cannot be null or whitespace.", nameof(reason));
        }

        return Create(ErrorCodes.Validation.InvalidFieldValue, reason, fieldName, new Dictionary<string, object?>
        {
            ["field"] = fieldName,
            ["reason"] = reason
        });
    }

    public static ErrorDetail NotFound(string resourceType, string? resourceId = null)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
        {
            throw new ArgumentException("Resource type cannot be null or whitespace.", nameof(resourceType));
        }

        return Create(ErrorCodes.NotFound.Resource, $"{resourceType} was not found.", resourceId, new Dictionary<string, object?>
        {
            ["resourceType"] = resourceType,
            ["resourceId"] = resourceId
        });
    }

    public static ErrorDetail Conflict(string resourceType, string? resourceId = null)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
        {
            throw new ArgumentException("Resource type cannot be null or whitespace.", nameof(resourceType));
        }

        return Create(ErrorCodes.Conflict.ResourceConflict, $"{resourceType} is in a conflicting state.", resourceId, new Dictionary<string, object?>
        {
            ["resourceType"] = resourceType,
            ["resourceId"] = resourceId
        });
    }

    public static ErrorDetail RateLimitExceeded(string message)
    {
        return Create("RateLimitExceeded", message, null, new Dictionary<string, object?>
        {
            ["statusCode"] = 429
        });
    }
}

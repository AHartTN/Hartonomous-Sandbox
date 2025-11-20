using Hartonomous.Core.Enums;

namespace Hartonomous.Core.Result;

/// <summary>
/// Represents the result of an operation that can succeed or fail.
/// Implements the Result/Either monad pattern for functional error handling.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public sealed class Result<T>
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the success value. Only valid if IsSuccess is true.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the error message. Only valid if IsSuccess is false.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Gets the error severity level.
    /// </summary>
    public ErrorSeverity Severity { get; }

    /// <summary>
    /// Gets the optional exception that caused the failure.
    /// </summary>
    public Exception? Exception { get; }

    private Result(bool isSuccess, T? value, string? error, ErrorSeverity severity, Exception? exception)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Severity = severity;
        Exception = exception;
    }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    public static Result<T> Success(T value) => new(true, value, null, ErrorSeverity.None, null);

    /// <summary>
    /// Creates a failure result with an error message.
    /// </summary>
    public static Result<T> Failure(string error, ErrorSeverity severity = ErrorSeverity.Error)
        => new(false, default, error, severity, null);

    /// <summary>
    /// Creates a failure result with an error message and exception.
    /// </summary>
    public static Result<T> Failure(string error, Exception exception, ErrorSeverity severity = ErrorSeverity.Error)
        => new(false, default, error, severity, exception);

    /// <summary>
    /// Transforms the success value if the result is successful.
    /// </summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
    {
        return IsSuccess && Value != null
            ? Result<TOut>.Success(mapper(Value))
            : Result<TOut>.Failure(Error ?? "Operation failed", Severity);
    }

    /// <summary>
    /// Transforms the success value to another Result if the result is successful.
    /// </summary>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder)
    {
        return IsSuccess && Value != null
            ? binder(Value)
            : Result<TOut>.Failure(Error ?? "Operation failed", Severity);
    }

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess && Value != null)
        {
            action(Value);
        }
        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure.
    /// </summary>
    public Result<T> OnFailure(Action<string> action)
    {
        if (IsFailure && Error != null)
        {
            action(Error);
        }
        return this;
    }

    /// <summary>
    /// Returns the value if successful, otherwise returns the default value.
    /// </summary>
    public T? GetValueOrDefault(T? defaultValue = default) => IsSuccess ? Value : defaultValue;

    /// <summary>
    /// Returns the value if successful, otherwise throws an exception.
    /// </summary>
    public T GetValueOrThrow()
    {
        if (!IsSuccess)
        {
            throw new InvalidOperationException(Error ?? "Operation failed");
        }
        return Value!;
    }
}

/// <summary>
/// Represents a result without a value (void operation).
/// </summary>
public sealed class Result
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error message. Only valid if IsSuccess is false.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Gets the error severity level.
    /// </summary>
    public ErrorSeverity Severity { get; }

    /// <summary>
    /// Gets the optional exception that caused the failure.
    /// </summary>
    public Exception? Exception { get; }

    private Result(bool isSuccess, string? error, ErrorSeverity severity, Exception? exception)
    {
        IsSuccess = isSuccess;
        Error = error;
        Severity = severity;
        Exception = exception;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new(true, null, ErrorSeverity.None, null);

    /// <summary>
    /// Creates a failure result with an error message.
    /// </summary>
    public static Result Failure(string error, ErrorSeverity severity = ErrorSeverity.Error)
        => new(false, error, severity, null);

    /// <summary>
    /// Creates a failure result with an error message and exception.
    /// </summary>
    public static Result Failure(string error, Exception exception, ErrorSeverity severity = ErrorSeverity.Error)
        => new(false, error, severity, exception);

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    public Result OnSuccess(Action action)
    {
        if (IsSuccess)
        {
            action();
        }
        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure.
    /// </summary>
    public Result OnFailure(Action<string> action)
    {
        if (IsFailure && Error != null)
        {
            action(Error);
        }
        return this;
    }
}

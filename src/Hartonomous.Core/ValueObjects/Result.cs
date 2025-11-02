using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Hartonomous.Core.ValueObjects;

/// <summary>
/// Generic result type for operations that can succeed or fail.
/// Provides railway-oriented programming pattern with type-safe error handling.
/// Eliminates exceptions for expected failures.
/// </summary>
/// <typeparam name="T">The type of the success value</typeparam>
public class Result<T>
{
    private readonly T? _value;
    private readonly List<string> _errors;

    private Result(T value)
    {
        _value = value;
        _errors = new List<string>();
        IsSuccess = true;
    }

    private Result(params string[] errors)
    {
        _value = default;
        _errors = errors?.ToList() ?? new List<string>();
        IsSuccess = false;
    }

    private Result(IEnumerable<string> errors)
    {
        _value = default;
        _errors = errors?.ToList() ?? new List<string>();
        IsSuccess = false;
    }

    /// <summary>
    /// True if the operation succeeded.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsSuccess { get; }

    /// <summary>
    /// True if the operation failed.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Errors))]
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// The success value (only valid when IsSuccess is true).
    /// </summary>
    public T? Value => IsSuccess ? _value : throw new InvalidOperationException("Cannot access Value when result is a failure");

    /// <summary>
    /// The error messages (only valid when IsFailure is true).
    /// </summary>
    public IReadOnlyList<string> Errors => _errors.AsReadOnly();

    /// <summary>
    /// Get the first error message.
    /// </summary>
    public string? FirstError => _errors.FirstOrDefault();

    /// <summary>
    /// Create a successful result.
    /// </summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Create a failed result with a single error.
    /// </summary>
    public static Result<T> Failure(string error) => new(error);

    /// <summary>
    /// Create a failed result with multiple errors.
    /// </summary>
    public static Result<T> Failure(params string[] errors) => new(errors);

    /// <summary>
    /// Create a failed result with an error collection.
    /// </summary>
    public static Result<T> Failure(IEnumerable<string> errors) => new(errors);

    /// <summary>
    /// Map the success value to a new type.
    /// </summary>
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        return IsSuccess
            ? Result<TNew>.Success(mapper(Value))
            : Result<TNew>.Failure(_errors);
    }

    /// <summary>
    /// Bind (flatMap) to chain operations that return Result.
    /// </summary>
    public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> binder)
    {
        return IsSuccess
            ? binder(Value)
            : Result<TNew>.Failure(_errors);
    }

    /// <summary>
    /// Match pattern for handling both success and failure cases.
    /// </summary>
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<IReadOnlyList<string>, TResult> onFailure)
    {
        return IsSuccess
            ? onSuccess(Value)
            : onFailure(Errors);
    }

    /// <summary>
    /// Execute an action if successful.
    /// </summary>
    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess)
        {
            action(Value);
        }
        return this;
    }

    /// <summary>
    /// Execute an action if failed.
    /// </summary>
    public Result<T> OnFailure(Action<IReadOnlyList<string>> action)
    {
        if (IsFailure)
        {
            action(Errors);
        }
        return this;
    }

    /// <summary>
    /// Get the value or a default if failed.
    /// </summary>
    public T ValueOr(T defaultValue) => IsSuccess ? Value : defaultValue;

    /// <summary>
    /// Get the value or compute a default if failed.
    /// </summary>
    public T ValueOr(Func<T> defaultValueProvider) => IsSuccess ? Value : defaultValueProvider();

    /// <summary>
    /// Implicit conversion from value to success result.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);
}

/// <summary>
/// Non-generic result for operations that don't return a value.
/// </summary>
public class Result
{
    private readonly List<string> _errors;

    private Result()
    {
        _errors = new List<string>();
        IsSuccess = true;
    }

    private Result(params string[] errors)
    {
        _errors = errors?.ToList() ?? new List<string>();
        IsSuccess = false;
    }

    private Result(IEnumerable<string> errors)
    {
        _errors = errors?.ToList() ?? new List<string>();
        IsSuccess = false;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<string> Errors => _errors.AsReadOnly();
    public string? FirstError => _errors.FirstOrDefault();

    public static Result Success() => new();
    public static Result Failure(string error) => new(error);
    public static Result Failure(params string[] errors) => new(errors);
    public static Result Failure(IEnumerable<string> errors) => new(errors);

    public Result OnSuccess(Action action)
    {
        if (IsSuccess)
        {
            action();
        }
        return this;
    }

    public Result OnFailure(Action<IReadOnlyList<string>> action)
    {
        if (IsFailure)
        {
            action(Errors);
        }
        return this;
    }

    /// <summary>
    /// Convert to Result<T> with a value.
    /// </summary>
    public Result<T> WithValue<T>(T value) => IsSuccess
        ? Result<T>.Success(value)
        : Result<T>.Failure(_errors);
}

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hartonomous.Infrastructure.Logging;

/// <summary>
/// Extension methods for ILogger to provide consistent, structured logging patterns.
/// Eliminates 100+ instances of repeated logging code across the codebase.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Begins a logging scope for an operation with structured properties.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operationName">The operation being performed.</param>
    /// <param name="properties">Additional context properties as key-value pairs.</param>
    /// <returns>A disposable scope.</returns>
    /// <example>
    /// using (_logger.BeginOperationScope("EmbeddingGeneration", ("AtomId", 123), ("TenantId", 5)))
    /// {
    ///     // All logs within this scope will have these properties
    /// }
    /// </example>
    public static IDisposable? BeginOperationScope(
        this ILogger logger,
        string operationName,
        params (string Key, object? Value)[] properties)
    {
        var scopeDict = new Dictionary<string, object?> { ["Operation"] = operationName };

        foreach (var (key, value) in properties)
        {
            scopeDict[key] = value;
        }

        return logger.BeginScope(scopeDict);
    }

    /// <summary>
    /// Logs the start of an operation with structured context.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operation">The operation name.</param>
    /// <param name="context">Context properties as key-value pairs.</param>
    public static void LogOperationStart(
        this ILogger logger,
        string operation,
        params (string Key, object? Value)[] context)
    {
        if (!logger.IsEnabled(LogLevel.Information))
            return;

        var contextString = FormatContext(context);
        logger.LogInformation("Starting {Operation}{Context}", operation, contextString);
    }

    /// <summary>
    /// Logs the successful completion of an operation with duration.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operation">The operation name.</param>
    /// <param name="duration">The operation duration.</param>
    /// <param name="context">Context properties as key-value pairs.</param>
    public static void LogOperationComplete(
        this ILogger logger,
        string operation,
        TimeSpan duration,
        params (string Key, object? Value)[] context)
    {
        if (!logger.IsEnabled(LogLevel.Information))
            return;

        var contextString = FormatContext(context);
        logger.LogInformation(
            "Completed {Operation} in {DurationMs}ms{Context}",
            operation,
            duration.TotalMilliseconds,
            contextString);
    }

    /// <summary>
    /// Logs the successful completion of an operation using a Stopwatch.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operation">The operation name.</param>
    /// <param name="stopwatch">The stopwatch measuring the operation.</param>
    /// <param name="context">Context properties as key-value pairs.</param>
    public static void LogOperationComplete(
        this ILogger logger,
        string operation,
        Stopwatch stopwatch,
        params (string Key, object? Value)[] context)
    {
        LogOperationComplete(logger, operation, stopwatch.Elapsed, context);
    }

    /// <summary>
    /// Logs the failure of an operation with exception details.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="ex">The exception that occurred.</param>
    /// <param name="operation">The operation name.</param>
    /// <param name="context">Context properties as key-value pairs.</param>
    public static void LogOperationFailed(
        this ILogger logger,
        Exception ex,
        string operation,
        params (string Key, object? Value)[] context)
    {
        var contextString = FormatContext(context);
        logger.LogError(ex, "Failed {Operation}{Context}", operation, contextString);
    }

    /// <summary>
    /// Logs a warning for an operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operation">The operation name.</param>
    /// <param name="message">The warning message.</param>
    /// <param name="context">Context properties as key-value pairs.</param>
    public static void LogOperationWarning(
        this ILogger logger,
        string operation,
        string message,
        params (string Key, object? Value)[] context)
    {
        if (!logger.IsEnabled(LogLevel.Warning))
            return;

        var contextString = FormatContext(context);
        logger.LogWarning("{Operation}: {Message}{Context}", operation, message, contextString);
    }

    /// <summary>
    /// Executes an operation within a timed logging scope, logging start, completion, and any failures.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="context">Context properties as key-value pairs.</param>
    /// <returns>The result of the operation.</returns>
    /// <example>
    /// var result = await _logger.ExecuteWithLoggingAsync(
    ///     "GenerateEmbedding",
    ///     async () => await GenerateEmbeddingAsync(atomId),
    ///     ("AtomId", atomId),
    ///     ("TenantId", tenantId));
    /// </example>
    public static async System.Threading.Tasks.Task<T> ExecuteWithLoggingAsync<T>(
        this ILogger logger,
        string operationName,
        Func<System.Threading.Tasks.Task<T>> operation,
        params (string Key, object? Value)[] context)
    {
        using (logger.BeginOperationScope(operationName, context))
        {
            logger.LogOperationStart(operationName, context);
            var sw = Stopwatch.StartNew();

            try
            {
                var result = await operation().ConfigureAwait(false);
                sw.Stop();
                logger.LogOperationComplete(operationName, sw, context);
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                logger.LogOperationFailed(ex, operationName, context);
                throw;
            }
        }
    }

    /// <summary>
    /// Executes an operation within a timed logging scope, logging start, completion, and any failures.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="context">Context properties as key-value pairs.</param>
    public static async System.Threading.Tasks.Task ExecuteWithLoggingAsync(
        this ILogger logger,
        string operationName,
        Func<System.Threading.Tasks.Task> operation,
        params (string Key, object? Value)[] context)
    {
        using (logger.BeginOperationScope(operationName, context))
        {
            logger.LogOperationStart(operationName, context);
            var sw = Stopwatch.StartNew();

            try
            {
                await operation().ConfigureAwait(false);
                sw.Stop();
                logger.LogOperationComplete(operationName, sw, context);
            }
            catch (Exception ex)
            {
                sw.Stop();
                logger.LogOperationFailed(ex, operationName, context);
                throw;
            }
        }
    }

    /// <summary>
    /// Logs diagnostic information useful for troubleshooting.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="properties">Diagnostic properties.</param>
    public static void LogDiagnostic(
        this ILogger logger,
        string message,
        params (string Key, object? Value)[] properties)
    {
        if (!logger.IsEnabled(LogLevel.Debug))
            return;

        var contextString = FormatContext(properties);
        logger.LogDebug("{Message}{Context}", message, contextString);
    }

    private static string FormatContext((string Key, object? Value)[] context)
    {
        if (context == null || context.Length == 0)
            return string.Empty;

        var formatted = string.Join(", ", context.Select(c => $"{c.Key}={c.Value}"));
        return $" [{formatted}]";
    }
}

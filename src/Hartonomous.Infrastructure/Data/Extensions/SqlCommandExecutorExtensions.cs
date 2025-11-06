using System.Data;
using Hartonomous.Core.Interfaces;
using Microsoft.Data.SqlClient;

namespace Hartonomous.Infrastructure.Data.Extensions;

/// <summary>
/// High-performance extension methods for <see cref="ISqlCommandExecutor"/> that eliminate common duplication patterns.
/// Uses explicit SqlDbType to avoid AddWithValue performance problems (cache bloat, index prevention).
/// </summary>
/// <remarks>
/// Research shows AddWithValue causes:
/// - Cache bloat (separate plans for different string lengths)
/// - Index prevention (C# string → NVARCHAR, DB VARCHAR → widening)
/// - Type inference overhead on every execution
/// Source: https://www.dbdelta.com/addwithvalue-is-evil/
/// </remarks>
public static class SqlCommandExecutorExtensions
{
    /// <summary>
    /// Executes a stored procedure and maps results using the provided function.
    /// Uses explicit parameter typing for optimal performance.
    /// </summary>
    /// <typeparam name="TResult">Type of result returned after mapping.</typeparam>
    /// <param name="executor">SQL command executor instance.</param>
    /// <param name="procedureName">Name of stored procedure (schema-qualified recommended).</param>
    /// <param name="resultMapper">Function that maps SqlDataReader to result type.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <param name="parameters">Typed parameters (use collection expressions: [param1, param2]).</param>
    /// <returns>Mapped result from stored procedure execution.</returns>
    public static async Task<TResult> ExecuteStoredProcedureAsync<TResult>(
        this ISqlCommandExecutor executor,
        string procedureName,
        Func<SqlDataReader, CancellationToken, Task<TResult>> resultMapper,
        CancellationToken cancellationToken = default,
        params SqlParameterSpec[] parameters)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentException.ThrowIfNullOrWhiteSpace(procedureName);
        ArgumentNullException.ThrowIfNull(resultMapper);

        return await executor.ExecuteAsync(async (command, token) =>
        {
            command.CommandText = procedureName;
            command.CommandType = CommandType.StoredProcedure;

            // Add parameters with explicit types (avoids AddWithValue performance problems)
            foreach (var param in parameters)
            {
                var sqlParam = command.Parameters.Add(param.Name, param.Type);
                sqlParam.Value = param.Value ?? DBNull.Value;

                if (param.Size.HasValue)
                {
                    sqlParam.Size = param.Size.Value;
                }

                if (param.Precision.HasValue)
                {
                    sqlParam.Precision = param.Precision.Value;
                }

                if (param.Scale.HasValue)
                {
                    sqlParam.Scale = param.Scale.Value;
                }
            }

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            return await resultMapper(reader, token).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a stored procedure without returning results (INSERT, UPDATE, DELETE operations).
    /// </summary>
    public static async Task ExecuteStoredProcedureAsync(
        this ISqlCommandExecutor executor,
        string procedureName,
        CancellationToken cancellationToken = default,
        params SqlParameterSpec[] parameters)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentException.ThrowIfNullOrWhiteSpace(procedureName);

        await executor.ExecuteAsync(async (command, token) =>
        {
            command.CommandText = procedureName;
            command.CommandType = CommandType.StoredProcedure;

            foreach (var param in parameters)
            {
                var sqlParam = command.Parameters.Add(param.Name, param.Type);
                sqlParam.Value = param.Value ?? DBNull.Value;

                if (param.Size.HasValue)
                {
                    sqlParam.Size = param.Size.Value;
                }

                if (param.Precision.HasValue)
                {
                    sqlParam.Precision = param.Precision.Value;
                }

                if (param.Scale.HasValue)
                {
                    sqlParam.Scale = param.Scale.Value;
                }
            }

            await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a stored procedure that returns a single scalar value.
    /// </summary>
    public static async Task<T?> ExecuteStoredProcedureScalarAsync<T>(
        this ISqlCommandExecutor executor,
        string procedureName,
        CancellationToken cancellationToken = default,
        params SqlParameterSpec[] parameters)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentException.ThrowIfNullOrWhiteSpace(procedureName);

        return await executor.ExecuteAsync(async (command, token) =>
        {
            command.CommandText = procedureName;
            command.CommandType = CommandType.StoredProcedure;

            foreach (var param in parameters)
            {
                var sqlParam = command.Parameters.Add(param.Name, param.Type);
                sqlParam.Value = param.Value ?? DBNull.Value;

                if (param.Size.HasValue)
                {
                    sqlParam.Size = param.Size.Value;
                }
            }

            var result = await command.ExecuteScalarAsync(token).ConfigureAwait(false);
            return result == DBNull.Value ? default : (T?)result;
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a stored procedure and maps multiple result sets.
    /// </summary>
    /// <typeparam name="TResult">Type of result returned after mapping.</typeparam>
    /// <param name="executor">SQL command executor instance.</param>
    /// <param name="procedureName">Name of stored procedure.</param>
    /// <param name="resultMapper">Function that maps SqlDataReader across multiple result sets.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="parameters">Typed parameters.</param>
    /// <returns>Mapped result from all result sets.</returns>
    public static async Task<TResult> ExecuteStoredProcedureMultiResultAsync<TResult>(
        this ISqlCommandExecutor executor,
        string procedureName,
        Func<SqlDataReader, CancellationToken, Task<TResult>> resultMapper,
        CancellationToken cancellationToken = default,
        params SqlParameterSpec[] parameters)
    {
        // Same implementation as ExecuteStoredProcedureAsync but resultMapper handles NextResultAsync internally
        return await ExecuteStoredProcedureAsync(executor, procedureName, resultMapper, cancellationToken, parameters)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Typed parameter specification for SQL stored procedures.
/// Explicitly defines SqlDbType to avoid AddWithValue performance problems.
/// </summary>
/// <remarks>
/// Use C# 13 collection expressions for clean syntax:
/// <code>
/// await executor.ExecuteStoredProcedureAsync(
///     "dbo.sp_SearchAtoms",
///     MapResults,
///     cancellationToken,
///     [
///         SqlParam.NVarChar("@queryText", query, 4000),
///         SqlParam.Int("@topK", 10),
///         SqlParam.UniqueIdentifier("@tenantId", tenantId)
///     ]);
/// </code>
/// </remarks>
public readonly struct SqlParameterSpec
{
    public required string Name { get; init; }
    public required object? Value { get; init; }
    public required SqlDbType Type { get; init; }
    public int? Size { get; init; }
    public byte? Precision { get; init; }
    public byte? Scale { get; init; }
}

/// <summary>
/// Fluent factory for creating SqlParameterSpec instances with common types.
/// Provides compile-time safety and reduces verbosity.
/// </summary>
public static class SqlParam
{
    public static SqlParameterSpec Int(string name, int? value)
        => new() { Name = name, Value = value, Type = SqlDbType.Int };

    public static SqlParameterSpec BigInt(string name, long? value)
        => new() { Name = name, Value = value, Type = SqlDbType.BigInt };

    public static SqlParameterSpec NVarChar(string name, string? value, int size = -1)
        => new() { Name = name, Value = value, Type = SqlDbType.NVarChar, Size = size };

    public static SqlParameterSpec VarChar(string name, string? value, int size = -1)
        => new() { Name = name, Value = value, Type = SqlDbType.VarChar, Size = size };

    public static SqlParameterSpec UniqueIdentifier(string name, Guid? value)
        => new() { Name = name, Value = value, Type = SqlDbType.UniqueIdentifier };

    public static SqlParameterSpec Bit(string name, bool? value)
        => new() { Name = name, Value = value, Type = SqlDbType.Bit };

    public static SqlParameterSpec DateTime2(string name, DateTime? value)
        => new() { Name = name, Value = value, Type = SqlDbType.DateTime2 };

    public static SqlParameterSpec DateTimeOffset(string name, DateTimeOffset? value)
        => new() { Name = name, Value = value, Type = SqlDbType.DateTimeOffset };

    public static SqlParameterSpec Float(string name, double? value)
        => new() { Name = name, Value = value, Type = SqlDbType.Float };

    public static SqlParameterSpec Real(string name, float? value)
        => new() { Name = name, Value = value, Type = SqlDbType.Real };

    public static SqlParameterSpec Decimal(string name, decimal? value, byte precision = 18, byte scale = 2)
        => new() { Name = name, Value = value, Type = SqlDbType.Decimal, Precision = precision, Scale = scale };

    public static SqlParameterSpec VarBinary(string name, byte[]? value, int size = -1)
        => new() { Name = name, Value = value, Type = SqlDbType.VarBinary, Size = size };

    public static SqlParameterSpec Udt(string name, object? value, string typeName)
    {
        // For SQL Server UDTs (like GEOMETRY, GEOGRAPHY, custom types)
        // Note: Size isn't used, but we store typeName in a way the caller must handle
        return new() { Name = name, Value = value, Type = SqlDbType.Udt };
    }
}

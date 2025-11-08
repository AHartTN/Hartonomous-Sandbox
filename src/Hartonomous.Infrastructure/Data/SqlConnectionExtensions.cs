using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Data;

/// <summary>
/// Extension methods for SqlConnection to reduce boilerplate and improve consistency.
/// Eliminates 100+ instances of repeated connection handling code.
/// </summary>
public static class SqlConnectionExtensions
{
    /// <summary>
    /// Creates and opens a SQL connection from a connection string.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An open SqlConnection.</returns>
    /// <example>
    /// // BEFORE:
    /// await using var connection = new SqlConnection(_connectionString);
    /// await connection.OpenAsync(cancellationToken);
    /// 
    /// // AFTER:
    /// await using var connection = await _connectionString.CreateAndOpenAsync(cancellationToken);
    /// </example>
    public static async Task<SqlConnection> CreateAndOpenAsync(
        this string connectionString,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be empty", nameof(connectionString));

        var connection = new SqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Executes a stored procedure and returns a scalar result.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="connection">The SQL connection.</param>
    /// <param name="procedureName">The stored procedure name.</param>
    /// <param name="configureParameters">Action to configure command parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The scalar result.</returns>
    public static async Task<T?> ExecuteStoredProcedureScalarAsync<T>(
        this SqlConnection connection,
        string procedureName,
        Action<SqlCommand>? configureParameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = CreateStoredProcedureCommand(connection, procedureName);
        configureParameters?.Invoke(command);

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result is T typedResult ? typedResult : default;
    }

    /// <summary>
    /// Executes a stored procedure and processes results using a reader function.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="connection">The SQL connection.</param>
    /// <param name="procedureName">The stored procedure name.</param>
    /// <param name="readFunc">Function to read results from SqlDataReader.</param>
    /// <param name="configureParameters">Action to configure command parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result from the reader function.</returns>
    public static async Task<T> ExecuteStoredProcedureReaderAsync<T>(
        this SqlConnection connection,
        string procedureName,
        Func<SqlDataReader, Task<T>> readFunc,
        Action<SqlCommand>? configureParameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = CreateStoredProcedureCommand(connection, procedureName);
        configureParameters?.Invoke(command);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await readFunc(reader).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a stored procedure without returning results.
    /// </summary>
    /// <param name="connection">The SQL connection.</param>
    /// <param name="procedureName">The stored procedure name.</param>
    /// <param name="configureParameters">Action to configure command parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task ExecuteStoredProcedureAsync(
        this SqlConnection connection,
        string procedureName,
        Action<SqlCommand>? configureParameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = CreateStoredProcedureCommand(connection, procedureName);
        configureParameters?.Invoke(command);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a stored procedure and returns the number of affected rows.
    /// </summary>
    /// <param name="connection">The SQL connection.</param>
    /// <param name="procedureName">The stored procedure name.</param>
    /// <param name="configureParameters">Action to configure command parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public static async Task<int> ExecuteStoredProcedureNonQueryAsync(
        this SqlConnection connection,
        string procedureName,
        Action<SqlCommand>? configureParameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = CreateStoredProcedureCommand(connection, procedureName);
        configureParameters?.Invoke(command);

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds a parameter with value to the command, handling null values correctly.
    /// </summary>
    /// <param name="command">The SQL command.</param>
    /// <param name="parameterName">Parameter name (with @ prefix).</param>
    /// <param name="value">Parameter value (null becomes DBNull.Value).</param>
    /// <returns>The command for fluent chaining.</returns>
    public static SqlCommand AddParameterWithValue(
        this SqlCommand command,
        string parameterName,
        object? value)
    {
        command.Parameters.AddWithValue(parameterName, value ?? DBNull.Value);
        return command;
    }

    /// <summary>
    /// Adds an output parameter to the command.
    /// </summary>
    /// <param name="command">The SQL command.</param>
    /// <param name="parameterName">Parameter name (with @ prefix).</param>
    /// <param name="sqlDbType">The SQL data type.</param>
    /// <param name="size">Size for variable-length types (-1 for MAX).</param>
    /// <returns>The added parameter.</returns>
    public static SqlParameter AddOutputParameter(
        this SqlCommand command,
        string parameterName,
        SqlDbType sqlDbType,
        int size = -1)
    {
        var parameter = command.Parameters.Add(parameterName, sqlDbType, size);
        parameter.Direction = ParameterDirection.Output;
        return parameter;
    }

    /// <summary>
    /// Gets the value of an output parameter, handling DBNull.
    /// </summary>
    /// <typeparam name="T">The expected type.</typeparam>
    /// <param name="parameter">The output parameter.</param>
    /// <returns>The parameter value, or default(T) if DBNull.</returns>
    public static T? GetOutputValue<T>(this SqlParameter parameter)
    {
        return parameter.Value is DBNull ? default : (T?)parameter.Value;
    }

    private static SqlCommand CreateStoredProcedureCommand(SqlConnection connection, string procedureName)
    {
        return new SqlCommand(procedureName, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 300 // 5 minutes default for long-running operations
        };
    }
}

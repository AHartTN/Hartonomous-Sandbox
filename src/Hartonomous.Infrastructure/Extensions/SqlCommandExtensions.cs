using System.Data;
using Microsoft.Data.SqlClient;

namespace Hartonomous.Infrastructure.Extensions;

/// <summary>
/// Extension methods for SqlCommand to reduce boilerplate and enforce best practices.
/// Eliminates 100+ duplicate AddWithValue calls across the codebase.
/// </summary>
public static class SqlCommandExtensions
{
    /// <summary>
    /// Adds parameter with proper SqlDbType inference and null handling.
    /// Safer than AddWithValue which can cause implicit conversion issues.
    /// </summary>
    public static SqlCommand AddParameter<T>(this SqlCommand command, string name, T? value, SqlDbType? dbType = null)
    {
        var parameter = new SqlParameter(name, value ?? (object)DBNull.Value);

        if (dbType.HasValue)
        {
            parameter.SqlDbType = dbType.Value;
        }
        else
        {
            // Infer SqlDbType from T to avoid implicit conversion
            parameter.SqlDbType = InferSqlDbType<T>();
        }

        command.Parameters.Add(parameter);
        return command;
    }

    /// <summary>
    /// Adds multiple parameters in fluent style.
    /// Usage: cmd.AddParameters(("@Id", id), ("@Name", name))
    /// </summary>
    public static SqlCommand AddParameters(this SqlCommand command, params (string Name, object? Value)[] parameters)
    {
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
        return command;
    }

    /// <summary>
    /// Adds output parameter with specified type and optional size.
    /// </summary>
    public static SqlCommand AddOutputParameter(this SqlCommand command, string name, SqlDbType type, int size = 0)
    {
        var parameter = new SqlParameter(name, type)
        {
            Direction = ParameterDirection.Output
        };

        if (size > 0)
        {
            parameter.Size = size;
        }

        command.Parameters.Add(parameter);
        return command;
    }

    /// <summary>
    /// Adds return value parameter (for stored procedures).
    /// </summary>
    public static SqlCommand AddReturnParameter(this SqlCommand command, string name = "@ReturnValue")
    {
        var parameter = new SqlParameter(name, SqlDbType.Int)
        {
            Direction = ParameterDirection.ReturnValue
        };

        command.Parameters.Add(parameter);
        return command;
    }

    /// <summary>
    /// Gets typed output parameter value with null safety.
    /// </summary>
    public static T? GetOutputValue<T>(this SqlCommand command, string name)
    {
        var value = command.Parameters[name].Value;
        if (value == null || value == DBNull.Value)
            return default;

        return (T)value;
    }

    /// <summary>
    /// Gets return value from stored procedure.
    /// </summary>
    public static int GetReturnValue(this SqlCommand command, string name = "@ReturnValue")
    {
        return (int)(command.Parameters[name].Value ?? 0);
    }

    /// <summary>
    /// Executes command and returns single scalar value with type safety.
    /// </summary>
    public static async Task<T?> ExecuteScalarAsync<T>(this SqlCommand command, CancellationToken cancellationToken = default)
    {
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result == null || result == DBNull.Value)
            return default;

        return (T)result;
    }

    /// <summary>
    /// Executes command and maps results to objects using provided selector.
    /// </summary>
    public static async Task<List<T>> ExecuteQueryAsync<T>(
        this SqlCommand command,
        Func<SqlDataReader, T> selector,
        CancellationToken cancellationToken = default)
    {
        var results = new List<T>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(selector(reader));
        }

        return results;
    }

    /// <summary>
    /// Executes command and returns first result or default.
    /// </summary>
    public static async Task<T?> ExecuteQueryFirstOrDefaultAsync<T>(
        this SqlCommand command,
        Func<SqlDataReader, T> selector,
        CancellationToken cancellationToken = default)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return selector(reader);
        }

        return default;
    }

    private static SqlDbType InferSqlDbType<T>()
    {
        var type = typeof(T);
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(byte[]))
            return SqlDbType.VarBinary;

        return underlyingType.Name switch
        {
            nameof(Int32) => SqlDbType.Int,
            nameof(Int64) => SqlDbType.BigInt,
            nameof(Int16) => SqlDbType.SmallInt,
            nameof(Byte) => SqlDbType.TinyInt,
            nameof(Boolean) => SqlDbType.Bit,
            nameof(Decimal) => SqlDbType.Decimal,
            nameof(Double) => SqlDbType.Float,
            nameof(Single) => SqlDbType.Real,
            nameof(String) => SqlDbType.NVarChar,
            nameof(DateTime) => SqlDbType.DateTime2,
            nameof(DateTimeOffset) => SqlDbType.DateTimeOffset,
            nameof(Guid) => SqlDbType.UniqueIdentifier,
            _ => SqlDbType.Variant
        };
    }
}

using System.Data;
using Microsoft.Data.SqlClient;

namespace Hartonomous.Infrastructure.Extensions;

/// <summary>
/// Extension methods for SqlDataReader to reduce null checking boilerplate.
/// Eliminates 100+ IsDBNull checks and type casting patterns.
/// </summary>
public static class SqlDataReaderExtensions
{
    /// <summary>
    /// Gets value with null safety. Returns default(T) if column is DBNull.
    /// </summary>
    public static T? GetValueOrDefault<T>(this SqlDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return default;

        return (T)reader.GetValue(ordinal);
    }

    /// <summary>
    /// Gets value with null safety by column name.
    /// </summary>
    public static T? GetValueOrDefault<T>(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.GetValueOrDefault<T>(ordinal);
    }

    /// <summary>
    /// Gets string value or null if DBNull.
    /// </summary>
    public static string? GetStringOrNull(this SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    /// <summary>
    /// Gets string value or null if DBNull by column name.
    /// </summary>
    public static string? GetStringOrNull(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.GetStringOrNull(ordinal);
    }

    /// <summary>
    /// Gets int value or null if DBNull.
    /// </summary>
    public static int? GetInt32OrNull(this SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    /// <summary>
    /// Gets int value or null if DBNull by column name.
    /// </summary>
    public static int? GetInt32OrNull(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.GetInt32OrNull(ordinal);
    }

    /// <summary>
    /// Gets long value or null if DBNull.
    /// </summary>
    public static long? GetInt64OrNull(this SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    }

    /// <summary>
    /// Gets long value or null if DBNull by column name.
    /// </summary>
    public static long? GetInt64OrNull(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.GetInt64OrNull(ordinal);
    }

    /// <summary>
    /// Gets decimal value or null if DBNull.
    /// </summary>
    public static decimal? GetDecimalOrNull(this SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    }

    /// <summary>
    /// Gets decimal value or null if DBNull by column name.
    /// </summary>
    public static decimal? GetDecimalOrNull(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.GetDecimalOrNull(ordinal);
    }

    /// <summary>
    /// Gets DateTime value or null if DBNull.
    /// </summary>
    public static DateTime? GetDateTimeOrNull(this SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }

    /// <summary>
    /// Gets DateTime value or null if DBNull by column name.
    /// </summary>
    public static DateTime? GetDateTimeOrNull(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.GetDateTimeOrNull(ordinal);
    }

    /// <summary>
    /// Gets Guid value or null if DBNull.
    /// </summary>
    public static Guid? GetGuidOrNull(this SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
    }

    /// <summary>
    /// Gets Guid value or null if DBNull by column name.
    /// </summary>
    public static Guid? GetGuidOrNull(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.GetGuidOrNull(ordinal);
    }

    /// <summary>
    /// Gets bool value or null if DBNull.
    /// </summary>
    public static bool? GetBooleanOrNull(this SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetBoolean(ordinal);
    }

    /// <summary>
    /// Gets bool value or null if DBNull by column name.
    /// </summary>
    public static bool? GetBooleanOrNull(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.GetBooleanOrNull(ordinal);
    }

    /// <summary>
    /// Gets byte array or null if DBNull.
    /// </summary>
    public static byte[]? GetBytesOrNull(this SqlDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return null;

        var length = reader.GetBytes(ordinal, 0, null, 0, 0);
        var buffer = new byte[length];
        reader.GetBytes(ordinal, 0, buffer, 0, (int)length);
        return buffer;
    }

    /// <summary>
    /// Gets byte array or null if DBNull by column name.
    /// </summary>
    public static byte[]? GetBytesOrNull(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.GetBytesOrNull(ordinal);
    }

    /// <summary>
    /// Checks if column exists in result set.
    /// </summary>
    public static bool HasColumn(this SqlDataReader reader, string columnName)
    {
        try
        {
            reader.GetOrdinal(columnName);
            return true;
        }
        catch (IndexOutOfRangeException)
        {
            return false;
        }
    }

    /// <summary>
    /// Maps reader row to dictionary of column name -> value.
    /// Useful for dynamic queries or debugging.
    /// </summary>
    public static Dictionary<string, object?> ToDictionary(this SqlDataReader reader)
    {
        var dict = new Dictionary<string, object?>();

        for (int i = 0; i < reader.FieldCount; i++)
        {
            var name = reader.GetName(i);
            var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
            dict[name] = value;
        }

        return dict;
    }

    /// <summary>
    /// Reads all rows as dictionaries.
    /// </summary>
    public static async Task<List<Dictionary<string, object?>>> ToListAsync(
        this SqlDataReader reader,
        CancellationToken cancellationToken = default)
    {
        var results = new List<Dictionary<string, object?>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(reader.ToDictionary());
        }

        return results;
    }
}

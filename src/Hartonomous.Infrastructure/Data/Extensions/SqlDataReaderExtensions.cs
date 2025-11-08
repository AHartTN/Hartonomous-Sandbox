using Microsoft.Data.SqlClient;
using System.Data;

namespace Hartonomous.Infrastructure.Data.Extensions;

/// <summary>
/// Modern, high-performance extension methods for SqlDataReader using GetFieldValue&lt;T&gt;.
/// Provides null-safe access with both ordinal and name-based lookups.
/// </summary>
/// <remarks>
/// Uses GetFieldValue&lt;T&gt; (recommended by Microsoft) instead of legacy Get* methods for:
/// - Compile-time type safety
/// - Support for modern types (DateOnly, TimeOnly, etc.)
/// - Consistent null handling
/// Source: https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqldatareader.getfieldvalue
/// </remarks>
public static class SqlDataReaderExtensions
{
    #region Ordinal-based Access (Performance-Critical Paths)

    /// <summary>
    /// Gets a non-nullable Int32 value by ordinal position.
    /// </summary>
    /// <exception cref="SqlNullValueException">Thrown if database value is NULL.</exception>
    public static int GetInt32(this SqlDataReader reader, int ordinal)
        => reader.GetFieldValue<int>(ordinal);

    /// <summary>
    /// Gets a nullable Int32 value by ordinal position. Returns null if database value is NULL.
    /// </summary>
    public static int? GetInt32OrNull(this SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<int>(ordinal);

    /// <summary>
    /// Gets a non-nullable Int64 value by ordinal position.
    /// </summary>
    public static long GetInt64(this SqlDataReader reader, int ordinal)
        => reader.GetFieldValue<long>(ordinal);

    /// <summary>
    /// Gets a nullable Int64 value by ordinal position.
    /// </summary>
    public static long? GetInt64OrNull(this SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<long>(ordinal);

    /// <summary>
    /// Gets a non-nullable string value by ordinal position.
    /// </summary>
    public static string GetString(this SqlDataReader reader, int ordinal)
        => reader.GetFieldValue<string>(ordinal);

    /// <summary>
    /// Gets a nullable string value by ordinal position. Returns null if database value is NULL.
    /// </summary>
    public static string? GetStringOrNull(this SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<string>(ordinal);

    /// <summary>
    /// Gets a non-nullable Guid value by ordinal position.
    /// </summary>
    public static Guid GetGuid(this SqlDataReader reader, int ordinal)
        => reader.GetFieldValue<Guid>(ordinal);

    /// <summary>
    /// Gets a nullable Guid value by ordinal position.
    /// </summary>
    public static Guid? GetGuidOrNull(this SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<Guid>(ordinal);

    /// <summary>
    /// Gets a non-nullable DateTime value by ordinal position.
    /// </summary>
    public static DateTime GetDateTime(this SqlDataReader reader, int ordinal)
        => reader.GetFieldValue<DateTime>(ordinal);

    /// <summary>
    /// Gets a nullable DateTime value by ordinal position.
    /// </summary>
    public static DateTime? GetDateTimeOrNull(this SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<DateTime>(ordinal);

    /// <summary>
    /// Gets a non-nullable DateTimeOffset value by ordinal position.
    /// </summary>
    public static DateTimeOffset GetDateTimeOffset(this SqlDataReader reader, int ordinal)
        => reader.GetFieldValue<DateTimeOffset>(ordinal);

    /// <summary>
    /// Gets a nullable DateTimeOffset value by ordinal position.
    /// </summary>
    public static DateTimeOffset? GetDateTimeOffsetOrNull(this SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<DateTimeOffset>(ordinal);

    /// <summary>
    /// Gets a non-nullable bool value by ordinal position.
    /// </summary>
    public static bool GetBoolean(this SqlDataReader reader, int ordinal)
        => reader.GetFieldValue<bool>(ordinal);

    /// <summary>
    /// Gets a nullable bool value by ordinal position.
    /// </summary>
    public static bool? GetBooleanOrNull(this SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<bool>(ordinal);

    /// <summary>
    /// Gets a non-nullable decimal value by ordinal position.
    /// </summary>
    public static decimal GetDecimal(this SqlDataReader reader, int ordinal)
        => reader.GetFieldValue<decimal>(ordinal);

    /// <summary>
    /// Gets a nullable decimal value by ordinal position.
    /// </summary>
    public static decimal? GetDecimalOrNull(this SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<decimal>(ordinal);

    /// <summary>
    /// Gets a non-nullable double value by ordinal position.
    /// </summary>
    public static double GetDouble(this SqlDataReader reader, int ordinal)
        => reader.GetFieldValue<double>(ordinal);

    /// <summary>
    /// Gets a nullable double value by ordinal position.
    /// </summary>
    public static double? GetDoubleOrNull(this SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<double>(ordinal);

    /// <summary>
    /// Gets a non-nullable float value by ordinal position.
    /// </summary>
    public static float GetFloat(this SqlDataReader reader, int ordinal)
        => reader.GetFieldValue<float>(ordinal);

    /// <summary>
    /// Gets a nullable float value by ordinal position.
    /// </summary>
    public static float? GetFloatOrNull(this SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<float>(ordinal);

    /// <summary>
    /// Gets a non-nullable byte value by ordinal position.
    /// </summary>
    public static byte GetByte(this SqlDataReader reader, int ordinal)
        => reader.GetFieldValue<byte>(ordinal);

    /// <summary>
    /// Gets a nullable byte value by ordinal position.
    /// </summary>
    public static byte? GetByteOrNull(this SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<byte>(ordinal);

    /// <summary>
    /// Gets a non-nullable byte array by ordinal position.
    /// </summary>
    public static byte[] GetBytes(this SqlDataReader reader, int ordinal)
        => reader.GetFieldValue<byte[]>(ordinal);

    /// <summary>
    /// Gets a nullable byte array by ordinal position.
    /// </summary>
    public static byte[]? GetBytesOrNull(this SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<byte[]>(ordinal);

    #endregion

    #region Name-based Access (Readability-Focused Paths)

    /// <summary>
    /// Gets the ordinal position of a column by name. Throws if column not found.
    /// </summary>
    /// <remarks>
    /// Cache this result when calling repeatedly in loops - GetOrdinal is relatively expensive.
    /// </remarks>
    public static int GetOrdinalSafe(this SqlDataReader reader, string columnName)
    {
        try
        {
            return reader.GetOrdinal(columnName);
        }
        catch (IndexOutOfRangeException ex)
        {
            throw new ArgumentException($"Column '{columnName}' not found in result set. Available columns: {string.Join(", ", GetColumnNames(reader))}", nameof(columnName), ex);
        }
    }

    /// <summary>
    /// Gets all column names in the current result set.
    /// </summary>
    public static IEnumerable<string> GetColumnNames(this SqlDataReader reader)
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            yield return reader.GetName(i);
        }
    }

    /// <summary>
    /// Gets a non-nullable Int64 value by column name.
    /// </summary>
    /// <remarks>
    /// For performance-critical loops, use ordinal-based access instead (cache GetOrdinal result).
    /// </remarks>
    public static long GetInt64(this SqlDataReader reader, string columnName)
        => reader.GetInt64(reader.GetOrdinalSafe(columnName));

    /// <summary>
    /// Gets a nullable Int64 value by column name.
    /// </summary>
    public static long? GetInt64OrNull(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinalSafe(columnName);
        return reader.GetInt64OrNull(ordinal);
    }

    /// <summary>
    /// Gets a non-nullable string value by column name.
    /// </summary>
    public static string GetString(this SqlDataReader reader, string columnName)
        => reader.GetString(reader.GetOrdinalSafe(columnName));

    /// <summary>
    /// Gets a nullable string value by column name.
    /// </summary>
    public static string? GetStringOrNull(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinalSafe(columnName);
        return reader.GetStringOrNull(ordinal);
    }

    /// <summary>
    /// Gets a non-nullable Guid value by column name.
    /// </summary>
    public static Guid GetGuid(this SqlDataReader reader, string columnName)
        => reader.GetGuid(reader.GetOrdinalSafe(columnName));

    /// <summary>
    /// Gets a nullable Guid value by column name.
    /// </summary>
    public static Guid? GetGuidOrNull(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinalSafe(columnName);
        return reader.GetGuidOrNull(ordinal);
    }

    /// <summary>
    /// Gets a non-nullable DateTime value by column name.
    /// </summary>
    public static DateTime GetDateTime(this SqlDataReader reader, string columnName)
        => reader.GetDateTime(reader.GetOrdinalSafe(columnName));

    /// <summary>
    /// Gets a nullable DateTime value by column name.
    /// </summary>
    public static DateTime? GetDateTimeOrNull(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinalSafe(columnName);
        return reader.GetDateTimeOrNull(ordinal);
    }

    /// <summary>
    /// Gets a non-nullable bool value by column name.
    /// </summary>
    public static bool GetBoolean(this SqlDataReader reader, string columnName)
        => reader.GetBoolean(reader.GetOrdinalSafe(columnName));

    /// <summary>
    /// Gets a nullable bool value by column name.
    /// </summary>
    public static bool? GetBooleanOrNull(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinalSafe(columnName);
        return reader.GetBooleanOrNull(ordinal);
    }

    /// <summary>
    /// Gets a non-nullable double value by column name.
    /// </summary>
    public static double GetDouble(this SqlDataReader reader, string columnName)
        => reader.GetDouble(reader.GetOrdinalSafe(columnName));

    /// <summary>
    /// Gets a nullable double value by column name.
    /// </summary>
    public static double? GetDoubleOrNull(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinalSafe(columnName);
        return reader.GetDoubleOrNull(ordinal);
    }

    #endregion

    #region Collection Helpers (C# 13 Collection Expressions)

    /// <summary>
    /// Materializes all rows in the current result set into a list using the provided mapper.
    /// Uses modern async iteration pattern.
    /// </summary>
    /// <typeparam name="T">Type of object to map each row to.</typeparam>
    /// <param name="reader">Data reader positioned before first row.</param>
    /// <param name="mapper">Function that maps current row to T.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all mapped rows.</returns>
    public static async Task<List<T>> ToListAsync<T>(
        this SqlDataReader reader,
        Func<SqlDataReader, T> mapper,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(mapper);

        var results = new List<T>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(mapper(reader));
        }
        return results;
    }

    /// <summary>
    /// Materializes all rows in the current result set into a list using an async mapper.
    /// </summary>
    public static async Task<List<T>> ToListAsync<T>(
        this SqlDataReader reader,
        Func<SqlDataReader, CancellationToken, Task<T>> asyncMapper,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(asyncMapper);

        var results = new List<T>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(await asyncMapper(reader, cancellationToken).ConfigureAwait(false));
        }
        return results;
    }

    /// <summary>
    /// Reads a single row and maps it, or returns null if no rows exist.
    /// </summary>
    public static async Task<T?> SingleOrDefaultAsync<T>(
        this SqlDataReader reader,
        Func<SqlDataReader, T> mapper,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(mapper);

        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return mapper(reader);
        }
        return null;
    }

    /// <summary>
    /// Reads the first row and maps it, or returns null if no rows exist.
    /// </summary>
    public static async Task<T?> FirstOrDefaultAsync<T>(
        this SqlDataReader reader,
        Func<SqlDataReader, T> mapper,
        CancellationToken cancellationToken = default) where T : class
    {
        return await SingleOrDefaultAsync(reader, mapper, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Modern .NET Types (.NET 6+)

    /// <summary>
    /// Gets a non-nullable DateOnly value by ordinal position (.NET 6+).
    /// </summary>
    public static DateOnly GetDateOnly(this SqlDataReader reader, int ordinal)
        => reader.GetFieldValue<DateOnly>(ordinal);

    /// <summary>
    /// Gets a nullable DateOnly value by ordinal position (.NET 6+).
    /// </summary>
    public static DateOnly? GetDateOnlyOrNull(this SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<DateOnly>(ordinal);

    /// <summary>
    /// Gets a non-nullable TimeOnly value by ordinal position (.NET 6+).
    /// </summary>
    public static TimeOnly GetTimeOnly(this SqlDataReader reader, int ordinal)
        => reader.GetFieldValue<TimeOnly>(ordinal);

    /// <summary>
    /// Gets a nullable TimeOnly value by ordinal position (.NET 6+).
    /// </summary>
    public static TimeOnly? GetTimeOnlyOrNull(this SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<TimeOnly>(ordinal);

    #endregion
}

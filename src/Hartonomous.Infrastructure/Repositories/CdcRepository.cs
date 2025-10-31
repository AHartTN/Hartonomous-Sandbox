using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Change Data Capture (CDC) operations.
/// Provides access to SQL Server 2025 Change Event Streaming data.
/// </summary>
public class CdcRepository : ICdcRepository
{
    private readonly HartonomousDbContext _context;
    private readonly ILogger<CdcRepository> _logger;

    public CdcRepository(HartonomousDbContext context, ILogger<CdcRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IList<CdcChangeEvent>> GetChangeEventsSinceAsync(string? lastLsn, CancellationToken cancellationToken)
    {
        var events = new List<(SqlBinary Lsn, CdcChangeEvent Event)>();

        var connectionString = _context.Database.GetConnectionString();
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogError("Database connection string is not configured");
            return Array.Empty<CdcChangeEvent>();
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var trackedTables = await GetTrackedTablesAsync(connection, cancellationToken).ConfigureAwait(false);
        if (trackedTables.Count == 0)
        {
            _logger.LogWarning("No CDC tracked tables discovered. Ensure CDC is enabled and capture instances are configured.");
            return Array.Empty<CdcChangeEvent>();
        }

        var lastLsnBytes = ParseLsn(lastLsn);

        foreach (var tracked in trackedTables)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var changeTable = $"{tracked.CaptureInstance}_CT";
            var query = $@"
SELECT *
FROM [cdc].[{changeTable}]
WHERE (@lastLsn IS NULL OR __$start_lsn > @lastLsn)
ORDER BY __$start_lsn";

            await using var command = new SqlCommand(query, connection)
            {
                CommandTimeout = 60
            };

            var parameter = command.Parameters.Add("@lastLsn", System.Data.SqlDbType.VarBinary, 10);
            parameter.Value = lastLsnBytes ?? (object)DBNull.Value;

            try
            {
                await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                var startLsnOrdinal = reader.GetOrdinal("__$start_lsn");
                var operationOrdinal = reader.GetOrdinal("__$operation");

                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var lsnBinary = reader.GetSqlBinary(startLsnOrdinal);
                    var operation = reader.GetInt32(operationOrdinal);

                    var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        var columnName = reader.GetName(i);
                        if (columnName.StartsWith("__$", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        var value = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                        data[columnName] = value;
                    }

                    var changeEvent = new CdcChangeEvent
                    {
                        Lsn = FormatLsn(lsnBinary),
                        Operation = operation,
                        TableName = $"{tracked.SchemaName}.{tracked.TableName}",
                        Data = data
                    };

                    events.Add((lsnBinary, changeEvent));
                }
            }
            catch (SqlException ex) when (ex.Number == 208)
            {
                _logger.LogWarning(ex, "CDC change table not found for capture instance {Instance}", tracked.CaptureInstance);
            }
        }

        events.Sort(static (left, right) => left.Lsn.CompareTo(right.Lsn));

        _logger.LogInformation("Retrieved {Count} CDC change events since LSN {LastLsn}", events.Count, lastLsn ?? "beginning");
        return events.ConvertAll(static e => e.Event);
    }

    private byte[]? ParseLsn(string? lastLsn)
    {
        if (string.IsNullOrWhiteSpace(lastLsn))
        {
            return null;
        }

        var hex = (lastLsn.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? lastLsn[2..]
            : lastLsn).Trim();

        try
        {
            return Convert.FromHexString(hex);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Invalid LSN format provided: {Lsn}", lastLsn);
            return null;
        }
    }

    private static string FormatLsn(SqlBinary lsn)
    {
        return $"0x{Convert.ToHexString(lsn.Value)}";
    }

    private async Task<IReadOnlyList<(string SchemaName, string TableName, string CaptureInstance)>> GetTrackedTablesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var results = new List<(string SchemaName, string TableName, string CaptureInstance)>();

        const string trackedTablesSql = @"
SELECT DISTINCT
    s.name AS SchemaName,
    t.name AS TableName,
    c.capture_instance AS CaptureInstance
FROM cdc.change_tables c
INNER JOIN sys.tables t ON c.source_object_id = t.object_id
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
UNION
SELECT DISTINCT
    s.name AS SchemaName,
    t.name AS TableName,
    CONCAT(s.name, '_', t.name) AS CaptureInstance
FROM sys.change_tracking_tables ct
INNER JOIN sys.tables t ON ct.object_id = t.object_id
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id";

        await using var command = new SqlCommand(trackedTablesSql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schemaName = reader.GetString(0);
            var tableName = reader.GetString(1);
            var captureInstance = reader.GetString(2);
            results.Add((schemaName, tableName, captureInstance));
        }

        return results;
    }
}
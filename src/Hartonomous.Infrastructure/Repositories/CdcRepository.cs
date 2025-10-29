using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
        var events = new List<CdcChangeEvent>();

        // Use raw SQL to query CDC change table
        // Note: This is a specialized query that EF Core cannot easily express
        var query = @"
            SELECT
                ct.__$start_lsn,
                ct.__$operation,
                ct.__$update_mask,
                ct.*,
                cdc.fn_cdc_get_column_ordinal(ct.__$table_name, column_name) as column_ordinal
            FROM cdc.dbo_Models_CT ct
            WHERE ct.__$start_lsn > @lastLsn OR @lastLsn IS NULL
            ORDER BY ct.__$start_lsn";

        var connectionString = _context.Database.GetConnectionString();
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogError("Database connection string is not configured");
            return events;
        }

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@lastLsn", lastLsn ?? (object)DBNull.Value);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var lsn = reader.GetSqlBinary(0).ToString();
            var operation = reader.GetInt32(1);
            var tableName = "dbo.Models"; // Inferred from CT table name

            // Extract the actual data columns (skip CDC metadata columns)
            var data = new Dictionary<string, object>();
            for (int i = 5; i < reader.FieldCount - 1; i++) // Skip CDC columns and ordinal
            {
                var columnName = reader.GetName(i);
                if (!columnName.StartsWith("__$")) // Skip CDC internal columns
                {
                    var value = await reader.GetFieldValueAsync<object>(i, cancellationToken);
                    data[columnName] = value;
                }
            }

            events.Add(new CdcChangeEvent
            {
                Lsn = lsn,
                Operation = operation,
                TableName = tableName,
                Data = data
            });
        }

        _logger.LogInformation("Retrieved {Count} CDC change events since LSN {LastLsn}", events.Count, lastLsn ?? "beginning");
        return events;
    }
}
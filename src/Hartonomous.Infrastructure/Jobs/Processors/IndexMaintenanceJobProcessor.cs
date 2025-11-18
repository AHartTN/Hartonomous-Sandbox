using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Hartonomous.Data;
using Hartonomous.Data.Entities;
using Hartonomous.Data.Entities.Entities;

namespace Hartonomous.Infrastructure.Jobs.Processors;

/// <summary>
/// Payload for index maintenance jobs.
/// </summary>
public class IndexMaintenancePayload
{
    /// <summary>
    /// Schema names to include (null = all schemas).
    /// </summary>
    public List<string>? Schemas { get; set; }

    /// <summary>
    /// Table names to include (null = all tables).
    /// </summary>
    public List<string>? Tables { get; set; }

    /// <summary>
    /// Fragmentation threshold percentage for rebuild (default: 30%).
    /// </summary>
    public double RebuildThreshold { get; set; } = 30.0;

    /// <summary>
    /// Fragmentation threshold percentage for reorganize (default: 10%).
    /// </summary>
    public double ReorganizeThreshold { get; set; } = 10.0;

    /// <summary>
    /// Whether to update statistics after index maintenance.
    /// </summary>
    public bool UpdateStatistics { get; set; } = true;

    /// <summary>
    /// Maximum duration in minutes (0 = no limit).
    /// </summary>
    public int MaxDurationMinutes { get; set; } = 60;
}

/// <summary>
/// Result from index maintenance job.
/// </summary>
public class IndexMaintenanceResult
{
    public int IndexesAnalyzed { get; set; }
    public int IndexesRebuilt { get; set; }
    public int IndexesReorganized { get; set; }
    public int StatisticsUpdated { get; set; }
    public List<string> ProcessedIndexes { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Processes index maintenance jobs to optimize database performance.
/// </summary>
public class IndexMaintenanceJobProcessor : IJobProcessor<IndexMaintenancePayload>
{
    private readonly HartonomousDbContext _context;
    private readonly ILogger<IndexMaintenanceJobProcessor> _logger;

    public string JobType => "IndexMaintenance";

    public IndexMaintenanceJobProcessor(
        HartonomousDbContext context,
        ILogger<IndexMaintenanceJobProcessor> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<object?> ProcessAsync(
        IndexMaintenancePayload payload,
        JobExecutionContext context,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var result = new IndexMaintenanceResult();

        var timeoutCts = payload.MaxDurationMinutes > 0
            ? new CancellationTokenSource(TimeSpan.FromMinutes(payload.MaxDurationMinutes))
            : null;

        var combinedCts = timeoutCts != null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            // Get fragmented indexes
            var indexes = await GetFragmentedIndexesAsync(payload, combinedCts.Token);
            result.IndexesAnalyzed = indexes.Count;

            _logger.LogInformation("Found {Count} indexes requiring maintenance", indexes.Count);

            foreach (var index in indexes)
            {
                if (combinedCts.Token.IsCancellationRequested)
                {
                    _logger.LogWarning("Index maintenance cancelled/timed out after processing {Count} indexes",
                        result.IndexesRebuilt + result.IndexesReorganized);
                    break;
                }

                try
                {
                    if (index.FragmentationPercent >= payload.RebuildThreshold)
                    {
                        await RebuildIndexAsync(index, combinedCts.Token);
                        result.IndexesRebuilt++;
                        result.ProcessedIndexes.Add($"REBUILD: {index.Schema}.{index.Table}.{index.IndexName}");
                    }
                    else if (index.FragmentationPercent >= payload.ReorganizeThreshold)
                    {
                        await ReorganizeIndexAsync(index, combinedCts.Token);
                        result.IndexesReorganized++;
                        result.ProcessedIndexes.Add($"REORGANIZE: {index.Schema}.{index.Table}.{index.IndexName}");
                    }

                    if (payload.UpdateStatistics)
                    {
                        await UpdateStatisticsAsync(index, combinedCts.Token);
                        result.StatisticsUpdated++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing index {Schema}.{Table}.{IndexName}: {Message}",
                        index.Schema, index.Table, index.IndexName, ex.Message);
                }
            }

            result.Duration = DateTime.UtcNow - startTime;

            _logger.LogInformation(
                "Index maintenance completed: {Rebuilt} rebuilt, {Reorganized} reorganized, {Statistics} statistics updated in {DurationMs}ms",
                result.IndexesRebuilt, result.IndexesReorganized, result.StatisticsUpdated, result.Duration.TotalMilliseconds);

            return result;
        }
        finally
        {
            timeoutCts?.Dispose();
            combinedCts?.Dispose();
        }
    }

    private async Task<List<IndexInfo>> GetFragmentedIndexesAsync(
        IndexMaintenancePayload payload,
        CancellationToken cancellationToken)
    {
        var sql = @"
            SELECT 
                SCHEMA_NAME(t.schema_id) AS SchemaName,
                OBJECT_NAME(ips.object_id) AS TableName,
                i.name AS IndexName,
                ips.avg_fragmentation_in_percent AS FragmentationPercent,
                ips.page_count AS PageCount
            FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
            INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
            INNER JOIN sys.tables t ON ips.object_id = t.object_id
            WHERE ips.avg_fragmentation_in_percent > @ReorganizeThreshold
                AND ips.page_count > 100
                AND i.name IS NOT NULL";

        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@ReorganizeThreshold", payload.ReorganizeThreshold)
        };

        if (payload.Schemas != null && payload.Schemas.Count > 0)
        {
            var schemaFilter = string.Join(",", payload.Schemas.Select((s, i) => $"@Schema{i}"));
            sql += $" AND SCHEMA_NAME(t.schema_id) IN ({schemaFilter})";
            
            for (int i = 0; i < payload.Schemas.Count; i++)
            {
                parameters.Add(new SqlParameter($"@Schema{i}", payload.Schemas[i]));
            }
        }

        if (payload.Tables != null && payload.Tables.Count > 0)
        {
            var tableFilter = string.Join(",", payload.Tables.Select((t, i) => $"@Table{i}"));
            sql += $" AND OBJECT_NAME(ips.object_id) IN ({tableFilter})";
            
            for (int i = 0; i < payload.Tables.Count; i++)
            {
                parameters.Add(new SqlParameter($"@Table{i}", payload.Tables[i]));
            }
        }

        sql += " ORDER BY ips.avg_fragmentation_in_percent DESC";

        var connection = _context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddRange(parameters.ToArray());

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var indexes = new List<IndexInfo>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            indexes.Add(new IndexInfo
            {
                Schema = reader.GetString(0),
                Table = reader.GetString(1),
                IndexName = reader.GetString(2),
                FragmentationPercent = reader.GetDouble(3),
                PageCount = reader.GetInt64(4)
            });
        }

        return indexes;
    }

    private async Task RebuildIndexAsync(IndexInfo index, CancellationToken cancellationToken)
    {
        var sql = $"ALTER INDEX [{index.IndexName}] ON [{index.Schema}].[{index.Table}] REBUILD WITH (ONLINE = ON)";

        _logger.LogInformation("Rebuilding index {Schema}.{Table}.{IndexName} ({Fragmentation:F2}% fragmented)",
            index.Schema, index.Table, index.IndexName, index.FragmentationPercent);

        try
        {
            await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
        catch (SqlException ex) when (ex.Number == 2725) // ONLINE not supported
        {
            _logger.LogWarning("ONLINE rebuild not supported for {IndexName}, falling back to OFFLINE rebuild", index.IndexName);
            sql = $"ALTER INDEX [{index.IndexName}] ON [{index.Schema}].[{index.Table}] REBUILD";
            await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
    }

    private async Task ReorganizeIndexAsync(IndexInfo index, CancellationToken cancellationToken)
    {
        var sql = $"ALTER INDEX [{index.IndexName}] ON [{index.Schema}].[{index.Table}] REORGANIZE";

        _logger.LogInformation("Reorganizing index {Schema}.{Table}.{IndexName} ({Fragmentation:F2}% fragmented)",
            index.Schema, index.Table, index.IndexName, index.FragmentationPercent);

        await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private async Task UpdateStatisticsAsync(IndexInfo index, CancellationToken cancellationToken)
    {
        var sql = $"UPDATE STATISTICS [{index.Schema}].[{index.Table}] [{index.IndexName}] WITH FULLSCAN";

        _logger.LogDebug("Updating statistics for {Schema}.{Table}.{IndexName}",
            index.Schema, index.Table, index.IndexName);

        await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private class IndexInfo
    {
        public required string Schema { get; set; }
        public required string Table { get; set; }
        public required string IndexName { get; set; }
        public double FragmentationPercent { get; set; }
        public long PageCount { get; set; }
    }
}

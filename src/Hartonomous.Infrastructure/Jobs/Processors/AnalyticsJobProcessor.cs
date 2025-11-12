using System.Collections.Immutable;
using System.Text.Json;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Jobs.Processors;

/// <summary>
/// Payload for analytics aggregation jobs.
/// </summary>
public class AnalyticsJobPayload
{
    /// <summary>
    /// Analytics report type (e.g., "DailyUsage", "TenantMetrics", "ModelPerformance").
    /// </summary>
    public required string ReportType { get; set; }

    /// <summary>
    /// Start date for analytics period (UTC).
    /// </summary>
    public DateTime StartDateUtc { get; set; }

    /// <summary>
    /// End date for analytics period (UTC).
    /// </summary>
    public DateTime EndDateUtc { get; set; }

    /// <summary>
    /// Tenant ID filter (null = all tenants).
    /// </summary>
    public int? TenantId { get; set; }

    /// <summary>
    /// Additional report-specific parameters.
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }
}

/// <summary>
/// Immutable representation of a tenant's daily usage measurements.
/// </summary>
public readonly record struct DailyUsageRow(
    int TenantId,
    DateTime UsageDate,
    int RequestCount,
    int SuccessfulRequests,
    int FailedRequests,
    int? AvgDurationMs);

/// <summary>
/// Immutable aggregate metrics per tenant.
/// </summary>
public readonly record struct TenantMetricsRow(
    int TenantId,
    string? TenantName,
    int TotalAtoms,
    int TotalEmbeddings,
    int TotalInferences,
    decimal TotalBillingAmount,
    DateTime? LastActivityUtc);

/// <summary>
/// Immutable performance summary for a deployed model.
/// </summary>
public readonly record struct ModelPerformanceRow(
    int ModelId,
    string? ModelName,
    int InferenceCount,
    double? AvgConfidence,
    int? AvgDurationMs,
    int SuccessCount,
    int FailureCount);

/// <summary>
/// Immutable inference statistics grouped by task and status.
/// </summary>
public readonly record struct InferenceStatsRow(
    string? InferenceTask,
    string? Status,
    int RequestCount,
    double? AvgConfidence,
    int? AvgDurationMs,
    DateTime? FirstRequestUtc,
    DateTime? LastRequestUtc);

/// <summary>
/// Result from analytics job execution.
/// </summary>
public class AnalyticsJobResult
{
    public string ReportType { get; set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; set; }
    public object? Data { get; set; }
    public Dictionary<string, long> RowCounts { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Processes analytics jobs to generate reports and metrics.
/// </summary>
public class AnalyticsJobProcessor : IJobProcessor<AnalyticsJobPayload>
{
    private readonly HartonomousDbContext _context;
    private readonly ILogger<AnalyticsJobProcessor> _logger;

    public string JobType => "Analytics";

    public AnalyticsJobProcessor(
        HartonomousDbContext context,
        ILogger<AnalyticsJobProcessor> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<object?> ProcessAsync(
        AnalyticsJobPayload payload,
        JobExecutionContext context,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("Generating analytics report '{ReportType}' for period {Start} to {End}",
            payload.ReportType, payload.StartDateUtc, payload.EndDateUtc);

        var result = new AnalyticsJobResult
        {
            ReportType = payload.ReportType,
            GeneratedAtUtc = startTime
        };

        result.Data = payload.ReportType switch
        {
            "DailyUsage" => await GenerateDailyUsageReportAsync(payload, result.RowCounts, cancellationToken),
            "TenantMetrics" => await GenerateTenantMetricsReportAsync(payload, result.RowCounts, cancellationToken),
            "ModelPerformance" => await GenerateModelPerformanceReportAsync(payload, result.RowCounts, cancellationToken),
            "InferenceStats" => await GenerateInferenceStatsReportAsync(payload, result.RowCounts, cancellationToken),
            _ => throw new ArgumentException($"Unknown report type: {payload.ReportType}")
        };

        result.Duration = DateTime.UtcNow - startTime;

        _logger.LogInformation("Analytics report '{ReportType}' generated in {DurationMs}ms: {RowCounts}",
            payload.ReportType, result.Duration.TotalMilliseconds,
            JsonSerializer.Serialize(result.RowCounts));

        return result;
    }

    private async Task<object> GenerateDailyUsageReportAsync(
        AnalyticsJobPayload payload,
        Dictionary<string, long> rowCounts,
        CancellationToken cancellationToken)
    {
        // Aggregate daily usage metrics by tenant
        var sql = @"
                SELECT 
                    TenantId,
                    CAST(RequestTimestamp AS DATE) AS UsageDate,
                    COUNT(*) AS RequestCount,
                    SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END) AS SuccessfulRequests,
                    SUM(CASE WHEN Status = 'Failed' THEN 1 ELSE 0 END) AS FailedRequests,
                    AVG(DATEDIFF(MILLISECOND, RequestTimestamp, CompletedAt)) AS AvgDurationMs
                FROM dbo.InferenceRequests
                WHERE RequestTimestamp >= @StartDate AND RequestTimestamp < @EndDate";

        if (payload.TenantId.HasValue)
        {
            sql += " AND TenantId = @TenantId";
        }

        sql += " GROUP BY TenantId, CAST(RequestTimestamp AS DATE) ORDER BY UsageDate DESC";

        var connection = _context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@StartDate", payload.StartDateUtc));
        command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@EndDate", payload.EndDateUtc));

        if (payload.TenantId.HasValue)
        {
            command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@TenantId", payload.TenantId.Value));
        }

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var usageBuilder = ImmutableArray.CreateBuilder<DailyUsageRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            usageBuilder.Add(new DailyUsageRow(
                TenantId: reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                UsageDate: reader.GetDateTime(1),
                RequestCount: reader.GetInt32(2),
                SuccessfulRequests: reader.GetInt32(3),
                FailedRequests: reader.GetInt32(4),
                AvgDurationMs: reader.IsDBNull(5) ? null : reader.GetInt32(5)));
        }

        rowCounts["DailyUsage"] = usageBuilder.Count;
        return usageBuilder.MoveToImmutable();
    }

    private async Task<object> GenerateTenantMetricsReportAsync(
        AnalyticsJobPayload payload,
        Dictionary<string, long> rowCounts,
        CancellationToken cancellationToken)
    {
        // Aggregate metrics per tenant
        var sql = @"
            SELECT 
                t.TenantId,
                t.TenantName,
                COUNT(DISTINCT a.AtomId) AS TotalAtoms,
                COUNT(DISTINCT ae.EmbeddingId) AS TotalEmbeddings,
                COUNT(DISTINCT ir.InferenceId) AS TotalInferences,
                SUM(b.Amount) AS TotalBillingAmount,
                MAX(ir.RequestTimestamp) AS LastActivityUtc
            FROM dbo.Tenants t
            LEFT JOIN dbo.TenantAtoms ta ON t.TenantId = ta.TenantId
            LEFT JOIN dbo.Atoms a ON ta.AtomId = a.AtomId
            LEFT JOIN dbo.AtomEmbeddings ae ON t.TenantId = ae.TenantId
            LEFT JOIN dbo.InferenceRequests ir ON t.TenantId = ir.TenantId
                AND ir.RequestTimestamp >= @StartDate AND ir.RequestTimestamp < @EndDate
            LEFT JOIN dbo.BillingRecords b ON t.TenantId = b.TenantId
                AND b.Timestamp >= @StartDate AND b.Timestamp < @EndDate";

        if (payload.TenantId.HasValue)
        {
            sql += " WHERE t.TenantId = @TenantId";
        }

        sql += " GROUP BY t.TenantId, t.TenantName ORDER BY TotalInferences DESC";

        var connection = _context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@StartDate", payload.StartDateUtc));
        command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@EndDate", payload.EndDateUtc));

        if (payload.TenantId.HasValue)
        {
            command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@TenantId", payload.TenantId.Value));
        }

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var metricsBuilder = ImmutableArray.CreateBuilder<TenantMetricsRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            metricsBuilder.Add(new TenantMetricsRow(
                TenantId: reader.GetInt32(0),
                TenantName: reader.IsDBNull(1) ? null : reader.GetString(1),
                TotalAtoms: reader.GetInt32(2),
                TotalEmbeddings: reader.GetInt32(3),
                TotalInferences: reader.GetInt32(4),
                TotalBillingAmount: reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                LastActivityUtc: reader.IsDBNull(6) ? null : reader.GetDateTime(6)));
        }

        rowCounts["TenantMetrics"] = metricsBuilder.Count;
        return metricsBuilder.MoveToImmutable();
    }

    private async Task<object> GenerateModelPerformanceReportAsync(
        AnalyticsJobPayload payload,
        Dictionary<string, long> rowCounts,
        CancellationToken cancellationToken)
    {
        // Aggregate model performance metrics
        var sql = @"
            SELECT 
                m.ModelId,
                m.ModelName,
                COUNT(ir.InferenceId) AS InferenceCount,
                AVG(ir.ConfidenceScore) AS AvgConfidence,
                AVG(DATEDIFF(MILLISECOND, ir.RequestTimestamp, ir.CompletedAt)) AS AvgDurationMs,
                SUM(CASE WHEN ir.Status = 'Completed' THEN 1 ELSE 0 END) AS SuccessCount,
                SUM(CASE WHEN ir.Status = 'Failed' THEN 1 ELSE 0 END) AS FailureCount
            FROM dbo.Models m
            LEFT JOIN dbo.InferenceRequests ir ON m.ModelId = ir.ModelId
                AND ir.RequestTimestamp >= @StartDate AND ir.RequestTimestamp < @EndDate";

        if (payload.TenantId.HasValue)
        {
            sql += " AND ir.TenantId = @TenantId";
        }

        sql += " GROUP BY m.ModelId, m.ModelName ORDER BY InferenceCount DESC";

        var connection = _context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@StartDate", payload.StartDateUtc));
        command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@EndDate", payload.EndDateUtc));

        if (payload.TenantId.HasValue)
        {
            command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@TenantId", payload.TenantId.Value));
        }

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var performanceBuilder = ImmutableArray.CreateBuilder<ModelPerformanceRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            performanceBuilder.Add(new ModelPerformanceRow(
                ModelId: reader.GetInt32(0),
                ModelName: reader.IsDBNull(1) ? null : reader.GetString(1),
                InferenceCount: reader.GetInt32(2),
                AvgConfidence: reader.IsDBNull(3) ? null : reader.GetDouble(3),
                AvgDurationMs: reader.IsDBNull(4) ? null : reader.GetInt32(4),
                SuccessCount: reader.GetInt32(5),
                FailureCount: reader.GetInt32(6)));
        }

        rowCounts["ModelPerformance"] = performanceBuilder.Count;
        return performanceBuilder.MoveToImmutable();
    }

    private async Task<object> GenerateInferenceStatsReportAsync(
        AnalyticsJobPayload payload,
        Dictionary<string, long> rowCounts,
        CancellationToken cancellationToken)
    {
        // Aggregate inference statistics
        var sql = @"
            SELECT 
                Task AS InferenceTask,
                Status,
                COUNT(*) AS RequestCount,
                AVG(ConfidenceScore) AS AvgConfidence,
                AVG(DATEDIFF(MILLISECOND, RequestTimestamp, CompletedAt)) AS AvgDurationMs,
                MIN(RequestTimestamp) AS FirstRequestUtc,
                MAX(RequestTimestamp) AS LastRequestUtc
            FROM dbo.InferenceRequests
            WHERE RequestTimestamp >= @StartDate AND RequestTimestamp < @EndDate";

        if (payload.TenantId.HasValue)
        {
            sql += " AND TenantId = @TenantId";
        }

        sql += " GROUP BY Task, Status ORDER BY RequestCount DESC";

        var connection = _context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@StartDate", payload.StartDateUtc));
        command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@EndDate", payload.EndDateUtc));

        if (payload.TenantId.HasValue)
        {
            command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@TenantId", payload.TenantId.Value));
        }

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var statsBuilder = ImmutableArray.CreateBuilder<InferenceStatsRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            statsBuilder.Add(new InferenceStatsRow(
                InferenceTask: reader.IsDBNull(0) ? null : reader.GetString(0),
                Status: reader.IsDBNull(1) ? null : reader.GetString(1),
                RequestCount: reader.GetInt32(2),
                AvgConfidence: reader.IsDBNull(3) ? null : reader.GetDouble(3),
                AvgDurationMs: reader.IsDBNull(4) ? null : reader.GetInt32(4),
                FirstRequestUtc: reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                LastRequestUtc: reader.IsDBNull(6) ? null : reader.GetDateTime(6)));
        }

        rowCounts["InferenceStats"] = statsBuilder.Count;
        return statsBuilder.MoveToImmutable();
    }
}

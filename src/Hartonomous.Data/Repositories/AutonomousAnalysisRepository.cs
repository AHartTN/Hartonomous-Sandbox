using Hartonomous.Core.Entities;
using Hartonomous.Core.Shared;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Repositories;

/// <summary>
/// Repository for autonomous analysis operations using EF Core
/// Replaces sp_Analyze stored procedure
/// </summary>
public class AutonomousAnalysisRepository : IAutonomousAnalysisRepository
{
    private readonly HartonomousDbContext _context;

    public AutonomousAnalysisRepository(HartonomousDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Performs system observation and analysis to detect anomalies and patterns
    /// </summary>
    public async Task<AnalysisResult> AnalyzeSystemAsync(
        int tenantId = 0,
        string analysisScope = "full",
        int lookbackHours = 24,
        CancellationToken cancellationToken = default)
    {
        var analysisId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        var lookbackStart = startTime.AddHours(-lookbackHours);

        // Query recent inference activity
        var recentInferences = await _context.InferenceRequests
            .Where(ir => ir.RequestTimestamp >= lookbackStart &&
                        (ir.Status == "Completed" || ir.Status == "Failed"))
            .OrderByDescending(ir => ir.RequestTimestamp)
            .Take(1000)
            .Select(ir => new
            {
                InferenceId = ir.InferenceId,
                ModelsUsed = ir.ModelsUsed, // JSON array of models
                RequestedAt = ir.RequestTimestamp,
                CompletedAt = ir.RequestTimestamp.AddMilliseconds(ir.TotalDurationMs ?? 0),
                DurationMs = ir.TotalDurationMs ?? 0,
                InputLength = ir.InputData != null ? ir.InputData.Length : 0,
                OutputLength = ir.OutputData != null ? ir.OutputData.Length : 0
            })
            .ToListAsync(cancellationToken);

        // Calculate token counts and parse model IDs from JSON
        var inferencesWithTokens = recentInferences.Select(ir =>
        {
            long? modelId = null;
            if (!string.IsNullOrEmpty(ir.ModelsUsed))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(ir.ModelsUsed);
                    if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array &&
                        doc.RootElement.GetArrayLength() > 0)
                    {
                        var firstModel = doc.RootElement[0];
                        if (firstModel.TryGetProperty("ModelId", out var modelIdProp) &&
                            modelIdProp.TryGetInt64(out var parsedId))
                        {
                            modelId = parsedId;
                        }
                    }
                }
                catch
                {
                    // Invalid JSON - keep modelId as null
                }
            }

            return new
            {
                ir.InferenceId,
                ModelId = modelId,
                ir.RequestedAt,
                ir.CompletedAt,
                ir.DurationMs,
                TokenCount = (ir.InputLength + ir.OutputLength) / 4
            };
        }).ToList();

        var totalInferences = inferencesWithTokens.Count;

        // Calculate average duration
        var avgDurationMs = inferencesWithTokens
            .Where(ir => ir.DurationMs > 0)
            .Average(ir => (double)ir.DurationMs);

        // Detect performance anomalies using SQL CLR IsolationForest aggregate
        // CLR aggregate computes contamination-based anomaly scores (0-1, >0.5 = anomaly)
        var anomalySql = @"
            WITH InferenceMetrics AS (
                SELECT 
                    InferenceId,
                    TotalDurationMs,
                    JSON_VALUE(ModelsUsed, '$[0].ModelId') AS ModelId
                FROM dbo.InferenceRequests
                WHERE RequestTimestamp >= @LookbackStart
                  AND Status IN ('Completed', 'Failed')
                  AND TotalDurationMs IS NOT NULL
            )
            SELECT 
                InferenceId,
                ModelId,
                TotalDurationMs,
                dbo.IsolationForest(
                    CAST(TotalDurationMs AS FLOAT), 
                    0.1, -- contamination rate (10% expected anomalies)
                    100  -- number of trees
                ) OVER () AS AnomalyScore
            FROM InferenceMetrics";
        
        var anomalies = new List<PerformanceAnomaly>();
        
        using var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);
        
        using var command = connection.CreateCommand();
        command.CommandText = anomalySql;
        
        var lookbackParam = command.CreateParameter();
        lookbackParam.ParameterName = "@LookbackStart";
        lookbackParam.Value = lookbackStart;
        command.Parameters.Add(lookbackParam);
        
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        while (await reader.ReadAsync(cancellationToken))
        {
            var anomalyScore = reader.GetDouble(3);
            
            // Threshold: anomaly score > 0.5 indicates outlier
            if (anomalyScore > 0.5)
            {
                var inferenceId = reader.GetInt64(0);
                var modelIdStr = reader.IsDBNull(1) ? null : reader.GetString(1);
                int? modelId = null;
                if (modelIdStr != null && int.TryParse(modelIdStr, out var parsed))
                    modelId = parsed;
                
                var durationMs = reader.GetInt64(2);
                
                anomalies.Add(new PerformanceAnomaly
                {
                    InferenceRequestId = inferenceId,
                    ModelId = modelId,
                    DurationMs = durationMs,
                    AvgDurationMs = avgDurationMs,
                    SlowdownFactor = durationMs / avgDurationMs
                });
            }
        }

        // Identify embedding patterns (clusters of similar embeddings)
        var patterns = await _context.AtomEmbeddings
            .Include(ae => ae.Atom)
            .Where(ae => ae.CreatedAt >= lookbackStart && ae.SpatialBucketX != null)
            .GroupBy(ae => new { ae.SpatialBucketX, ae.SpatialBucketY, ae.SpatialBucketZ })
            .Select(g => new
            {
                RepresentativeAtomId = g.First().AtomId,
                Modality = g.First().Atom!.Modality,
                ClusterSize = g.Count()
            })
            .OrderByDescending(x => x.ClusterSize)
            .Take(10)
            .Select(x => new EmbeddingPattern
            {
                AtomId = x.RepresentativeAtomId,
                Modality = x.Modality,
                ClusterSize = x.ClusterSize
            })
            .ToListAsync(cancellationToken);

        return new AnalysisResult
        {
            AnalysisId = analysisId,
            Scope = analysisScope,
            LookbackHours = lookbackHours,
            TotalInferences = totalInferences,
            AvgDurationMs = avgDurationMs,
            AnomalyCount = anomalies.Count,
            Anomalies = anomalies,
            Patterns = patterns,
            Timestamp = DateTime.UtcNow
        };
    }
}
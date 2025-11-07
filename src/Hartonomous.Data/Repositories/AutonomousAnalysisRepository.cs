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
                ModelId = (long?)null, // Simplified - would need JSON parsing for actual model IDs
                RequestedAt = ir.RequestTimestamp,
                CompletedAt = ir.RequestTimestamp.AddMilliseconds(ir.TotalDurationMs ?? 0),
                DurationMs = ir.TotalDurationMs ?? 0,
                InputLength = ir.InputData != null ? ir.InputData.Length : 0,
                OutputLength = ir.OutputData != null ? ir.OutputData.Length : 0
            })
            .ToListAsync(cancellationToken);

        // Calculate token counts after materializing the query
        var inferencesWithTokens = recentInferences.Select(ir => new
        {
            ir.InferenceId,
            ir.ModelId,
            ir.RequestedAt,
            ir.CompletedAt,
            ir.DurationMs,
            TokenCount = (ir.InputLength + ir.OutputLength) / 4
        }).ToList();

        var totalInferences = inferencesWithTokens.Count;

        // Calculate average duration
        var avgDurationMs = inferencesWithTokens
            .Where(ir => ir.DurationMs > 0)
            .Average(ir => (double)ir.DurationMs);

        // Detect performance anomalies (inferences that took >2x the average duration)
        var anomalies = inferencesWithTokens
            .Where(ir => ir.DurationMs > avgDurationMs * 2)
            .Select(ir => new PerformanceAnomaly
            {
                InferenceRequestId = ir.InferenceId,
                ModelId = ir.ModelId.HasValue ? (int)ir.ModelId.Value : null,
                DurationMs = ir.DurationMs,
                AvgDurationMs = avgDurationMs,
                SlowdownFactor = ir.DurationMs / avgDurationMs
            })
            .ToList();

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
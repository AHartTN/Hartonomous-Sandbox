using Hartonomous.Core.Entities;
using Hartonomous.Core.Shared;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System.Text.Json;

namespace Hartonomous.Data.Repositories;

/// <summary>
/// Repository for autonomous action operations using EF Core
/// Replaces sp_Act stored procedure
/// </summary>
public class AutonomousActionRepository : IAutonomousActionRepository
{
    private readonly HartonomousDbContext _context;
    private readonly IConceptDiscoveryRepository _conceptDiscovery;

    public AutonomousActionRepository(HartonomousDbContext context, IConceptDiscoveryRepository conceptDiscovery)
    {
        _context = context;
        _conceptDiscovery = conceptDiscovery;
    }

    /// <summary>
    /// Executes actions based on hypotheses received from analysis phase
    /// </summary>
    public async Task<ActionExecutionResult> ExecuteActionsAsync(
        Guid analysisId,
        IReadOnlyList<Hypothesis> hypotheses,
        int autoApproveThreshold = 3,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ActionResult>();

        // Process hypotheses in priority order
        var orderedHypotheses = hypotheses.OrderBy(h => h.Priority).ToList();

        foreach (var hypothesis in orderedHypotheses)
        {
            var startTime = DateTime.UtcNow;
            var actionResult = new ActionResult
            {
                HypothesisId = hypothesis.HypothesisId,
                HypothesisType = hypothesis.HypothesisType
            };

            try
            {
                string executedActions;
                string status;

                switch (hypothesis.HypothesisType)
                {
                    case "IndexOptimization":
                        (executedActions, status) = await ExecuteIndexOptimizationAsync(cancellationToken);
                        break;

                    case "CacheWarming":
                        (executedActions, status) = await ExecuteCacheWarmingAsync(cancellationToken);
                        break;

                    case "ConceptDiscovery":
                        (executedActions, status) = await ExecuteConceptDiscoveryAsync(cancellationToken);
                        break;

                    case "ModelRetraining":
                        (executedActions, status) = await ExecuteModelRetrainingAsync(hypothesis, cancellationToken);
                        break;

                    default:
                        executedActions = JsonSerializer.Serialize(new { status = "Skipped", reason = "Unknown hypothesis type" });
                        status = "Skipped";
                        break;
                }

                actionResult.ExecutedActions = executedActions;
                actionResult.ActionStatus = status;
            }
            catch (Exception ex)
            {
                actionResult.ActionStatus = "Failed";
                actionResult.ExecutedActions = JsonSerializer.Serialize(new { error = ex.Message });
                actionResult.ErrorMessage = ex.Message;
            }

            actionResult.ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            results.Add(actionResult);
        }

        return new ActionExecutionResult
        {
            AnalysisId = analysisId,
            ExecutedActions = results.Count(r => r.ActionStatus == "Executed"),
            QueuedActions = results.Count(r => r.ActionStatus == "QueuedForApproval"),
            FailedActions = results.Count(r => r.ActionStatus == "Failed"),
            Results = results,
            Timestamp = DateTime.UtcNow
        };
    }

    private async Task<(string executedActions, string status)> ExecuteIndexOptimizationAsync(CancellationToken cancellationToken)
    {
        // Query DMVs to find missing indexes
        var missingIndexSql = @"
            SELECT TOP 5
                d.statement AS TableName,
                d.equality_columns + COALESCE(', ' + d.inequality_columns, '') AS IndexColumns,
                s.avg_user_impact AS ImpactScore
            FROM sys.dm_db_missing_index_details d
            JOIN sys.dm_db_missing_index_groups g ON d.index_handle = g.index_handle
            JOIN sys.dm_db_missing_index_group_stats s ON g.index_group_handle = s.group_handle
            WHERE d.database_id = DB_ID()
                AND s.avg_user_impact > 50
            ORDER BY s.avg_user_impact DESC";

        using var conn = _context.Database.GetDbConnection();
        await conn.OpenAsync(cancellationToken);

        var missingIndexes = new List<object>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = missingIndexSql;
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                missingIndexes.Add(new
                {
                    TableName = reader.GetString(0),
                    IndexColumns = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    ImpactScore = reader.GetDouble(2)
                });
            }
        }

        // Update statistics on key tables
        var tablesToUpdate = new[] { "AtomEmbeddings", "InferenceRequests", "Atoms" };
        var statisticsResults = new List<string>();

        foreach (var tableName in tablesToUpdate)
        {
            try
            {
                // FormattableString prevents SQL injection - table names are hardcoded
                await _context.Database.ExecuteSqlAsync(
                    $"UPDATE STATISTICS dbo.{tableName} WITH FULLSCAN",
                    cancellationToken);
                statisticsResults.Add($"{tableName}: Success");
            }
            catch (Exception ex)
            {
                statisticsResults.Add($"{tableName}: Failed - {ex.Message}");
            }
        }

        var executedActions = JsonSerializer.Serialize(new
        {
            missingIndexesAnalyzed = missingIndexes.Count,
            potentialIndexes = missingIndexes,
            statisticsUpdated = statisticsResults
        });

        return (executedActions, "Executed");
    }

    private async Task<(string executedActions, string status)> ExecuteCacheWarmingAsync(CancellationToken cancellationToken)
    {
        // Identify frequently accessed embeddings from recent inference requests
        var frequentEmbeddingsSql = @"
            SELECT TOP 1000 ae.AtomEmbeddingId
            FROM dbo.AtomEmbeddings ae
            WHERE ae.CreatedAt >= DATEADD(day, -7, GETUTCDATE())
            ORDER BY ae.CreatedAt DESC";

        using var conn = _context.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        var embeddingIds = new List<long>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = frequentEmbeddingsSql;
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                embeddingIds.Add(reader.GetInt64(0));
            }
        }

        // Preload these embeddings into SQL Server buffer pool
        if (embeddingIds.Count > 0)
        {
            var warmUpSql = @"
                SELECT AtomEmbeddingId, EmbeddingVector, SpatialGeometry 
                FROM dbo.AtomEmbeddings WITH (READCOMMITTEDLOCK)
                WHERE AtomEmbeddingId IN (SELECT value FROM STRING_SPLIT(@Ids, ','))";

            using var cmd = conn.CreateCommand();
            cmd.CommandText = warmUpSql;
            var param = cmd.CreateParameter();
            param.ParameterName = "@Ids";
            param.Value = string.Join(",", embeddingIds.Take(1000));
            cmd.Parameters.Add(param);

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            var preloadedCount = 0;
            while (await reader.ReadAsync(cancellationToken))
            {
                preloadedCount++;
                // Just reading the data loads it into buffer pool
            }

            var executedActions = JsonSerializer.Serialize(new
            {
                preloadedEmbeddings = preloadedCount,
                candidateCount = embeddingIds.Count
            });
            return (executedActions, "Executed");
        }

        return (JsonSerializer.Serialize(new { preloadedEmbeddings = 0 }), "Executed");
    }

    private async Task<(string executedActions, string status)> ExecuteConceptDiscoveryAsync(CancellationToken cancellationToken)
    {
        // Call SQL CLR fn_DiscoverConcepts directly for DBSCAN clustering
        // No need to load embeddings into C# - database does all the work
        var discoverySql = @"
            SELECT 
                COUNT(*) AS ClustersFound,
                AVG(Coherence) AS AvgCoherence,
                SUM(AtomCount) AS TotalAtoms
            FROM dbo.fn_DiscoverConcepts(
                @MinClusterSize,
                @CoherenceThreshold,
                @MaxConcepts,
                @TenantId
            )";
        
        using var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);
        
        using var command = connection.CreateCommand();
        command.CommandText = discoverySql;
        
        var minSizeParam = command.CreateParameter();
        minSizeParam.ParameterName = "@MinClusterSize";
        minSizeParam.Value = 3;
        command.Parameters.Add(minSizeParam);
        
        var coherenceParam = command.CreateParameter();
        coherenceParam.ParameterName = "@CoherenceThreshold";
        coherenceParam.Value = 0.7;
        command.Parameters.Add(coherenceParam);
        
        var tenantParam = command.CreateParameter();
        tenantParam.ParameterName = "@TenantId";
        // TODO: Get actual TenantId from ambient context or pass as parameter
        // Use NULL for cross-tenant discovery (admin/analytics), or specific tenantId for tenant-isolated discovery
        // For now using default tenant 1 for background autonomous processing
        tenantParam.Value = 1;
        command.Parameters.Add(tenantParam);
        
        var maxConceptsParam = command.CreateParameter();
        maxConceptsParam.ParameterName = "@MaxConcepts";
        maxConceptsParam.Value = 50;
        command.Parameters.Add(maxConceptsParam);
        
        int clustersFound = 0;
        double avgCoherence = 0.0;
        int totalAtoms = 0;
        
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            clustersFound = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            avgCoherence = reader.IsDBNull(1) ? 0.0 : reader.GetDouble(1);
            totalAtoms = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
        }

        var executedActions = JsonSerializer.Serialize(new
        {
            discoveredClusters = clustersFound,
            clusteringQuality = avgCoherence,
            totalAtomsInClusters = totalAtoms,
            method = "dbscan_via_clr"
        });

        return (executedActions, "Executed");
    }

    private async Task<(string executedActions, string status)> ExecuteModelRetrainingAsync(Hypothesis hypothesis, CancellationToken cancellationToken)
    {
        // Queue for approval (dangerous operation)
        var executedActions = JsonSerializer.Serialize(new
        {
            status = "QueuedForApproval",
            reason = "ModelRetraining requires manual approval",
            hypothesisId = hypothesis.HypothesisId
        });

        // In real implementation, this would insert into an approval queue table
        return (executedActions, "QueuedForApproval");
    }
}
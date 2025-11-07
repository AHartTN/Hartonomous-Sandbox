using Hartonomous.Core.Entities;
using Hartonomous.Core.Shared;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Hartonomous.Data.Repositories;

/// <summary>
/// Repository for autonomous learning operations using EF Core
/// Replaces sp_Learn stored procedure
/// </summary>
public class AutonomousLearningRepository : IAutonomousLearningRepository
{
    private readonly HartonomousDbContext _context;

    public AutonomousLearningRepository(HartonomousDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Learns from system performance and stores improvement history
    /// </summary>
    public async Task<LearningResult> LearnFromPerformanceAsync(
        PerformanceMetrics performanceMetrics,
        IReadOnlyList<ActionResult> improvementActions,
        CancellationToken cancellationToken = default)
    {
        var learningId = Guid.NewGuid();
        var insights = new List<string>();
        var recommendations = new List<string>();
        var confidenceScore = 0.0;
        var isSystemHealthy = true;

        // Analyze performance trends
        var recentHistory = await _context.AutonomousImprovementHistory
            .Where(h => h.StartedAt >= DateTime.UtcNow.AddDays(-7))
            .OrderByDescending(h => h.StartedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        // Calculate performance trends
        var avgSuccessScore = recentHistory.Any() ?
            recentHistory.Where(h => h.SuccessScore.HasValue).Average(h => (double)h.SuccessScore!.Value) : 0;

        var avgPerformanceDelta = recentHistory.Any() ?
            recentHistory.Where(h => h.PerformanceDelta.HasValue).Average(h => (double)h.PerformanceDelta!.Value) : 0;

        // Generate insights based on current vs historical performance
        if (performanceMetrics.AverageResponseTimeMs > 100) // Simple threshold for now
        {
            insights.Add($"High response time detected: {performanceMetrics.AverageResponseTimeMs:F2}ms");
            recommendations.Add("Consider index optimization or query tuning");
            isSystemHealthy = false;
        }
        else
        {
            insights.Add($"Response time within acceptable range: {performanceMetrics.AverageResponseTimeMs:F2}ms");
        }

        if (performanceMetrics.Throughput < 10) // Simple threshold for now
        {
            insights.Add($"Low throughput detected: {performanceMetrics.Throughput:F2} req/sec");
            recommendations.Add("Investigate resource bottlenecks or scaling needs");
            isSystemHealthy = false;
        }

        // Analyze action effectiveness
        var successfulActions = improvementActions.Count(a => a.ActionStatus == "Executed");
        var failedActions = improvementActions.Count(a => a.ActionStatus == "Failed");

        if (failedActions > successfulActions)
        {
            insights.Add($"Action success rate: {(double)successfulActions / improvementActions.Count * 100:F1}%");
            recommendations.Add("Review action approval criteria or implementation");
            confidenceScore -= 0.2;
        }
        else
        {
            confidenceScore += 0.3;
        }

        // Calculate overall confidence
        confidenceScore = Math.Max(0.1, Math.Min(1.0, confidenceScore + 0.5));

        // Store improvement history
        var historyEntry = new AutonomousImprovementHistory
        {
            ImprovementId = learningId,
            AnalysisResults = JsonSerializer.Serialize(new
            {
                performanceMetrics,
                insights,
                recommendations,
                confidenceScore,
                isSystemHealthy
            }),
            GeneratedCode = JsonSerializer.Serialize(improvementActions.Select(a => new
            {
                a.HypothesisType,
                a.ActionStatus,
                a.ExecutionTimeMs
            })),
            TargetFile = "AutonomousLearning",
            ChangeType = "analysis",
            RiskLevel = "low",
            EstimatedImpact = "medium",
            SuccessScore = (decimal)confidenceScore,
            TestsPassed = successfulActions,
            TestsFailed = failedActions,
            PerformanceDelta = (decimal)(performanceMetrics.AverageResponseTimeMs / 100.0), // Simplified
            WasDeployed = true,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        _context.AutonomousImprovementHistory.Add(historyEntry);
        await _context.SaveChangesAsync(cancellationToken);

        return new LearningResult
        {
            LearningId = learningId,
            Insights = insights,
            Recommendations = recommendations,
            ConfidenceScore = confidenceScore,
            IsSystemHealthy = isSystemHealthy,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Restarts the OODA loop with learned insights
    /// </summary>
    public async Task<OODALoopConfiguration> RestartOODALoopAsync(
        LearningResult learningResult,
        CancellationToken cancellationToken = default)
    {
        // Analyze recent learning patterns to adjust configuration
        var recentLearnings = await _context.AutonomousImprovementHistory
            .Where(h => h.StartedAt >= DateTime.UtcNow.AddHours(-24))
            .ToListAsync(cancellationToken);

        var avgConfidence = recentLearnings.Any() ?
            recentLearnings.Where(h => h.SuccessScore.HasValue).Average(h => (double)h.SuccessScore!.Value) : 0.5;

        var systemHealthIssues = recentLearnings.Count(h => h.SuccessScore < 0.5m);

        // Adjust configuration based on learning patterns
        var config = new OODALoopConfiguration
        {
            Timestamp = DateTime.UtcNow
        };

        if (avgConfidence < 0.3)
        {
            // Low confidence - be more conservative
            config.AnalysisIntervalMinutes = 30;
            config.AutoApproveThreshold = 5;
            config.EnableAggressiveOptimization = false;
        }
        else if (systemHealthIssues > recentLearnings.Count / 2)
        {
            // Many health issues - be more aggressive
            config.AnalysisIntervalMinutes = 10;
            config.AutoApproveThreshold = 2;
            config.EnableAggressiveOptimization = true;
        }
        else
        {
            // Normal operation
            config.AnalysisIntervalMinutes = 15;
            config.AutoApproveThreshold = 3;
            config.EnableAggressiveOptimization = false;
        }

        // Set hypothesis priority weights based on recent effectiveness
        var priorityWeights = new Dictionary<string, int>
        {
            ["IndexOptimization"] = 1,
            ["CacheWarming"] = 2,
            ["ConceptDiscovery"] = 3,
            ["ModelRetraining"] = 5 // Highest priority for dangerous operations
        };

        // Adjust weights based on recent action success
        if (recentLearnings.Any())
        {
            var successfulIndexOpts = recentLearnings.Count(h =>
                h.GeneratedCode?.Contains("IndexOptimization") == true &&
                h.SuccessScore >= 0.7m);

            if (successfulIndexOpts > recentLearnings.Count / 2)
            {
                priorityWeights["IndexOptimization"] = 0; // Lower priority if already effective
            }
        }

        config.HypothesisPriorityWeights = priorityWeights;

        return config;
    }
}
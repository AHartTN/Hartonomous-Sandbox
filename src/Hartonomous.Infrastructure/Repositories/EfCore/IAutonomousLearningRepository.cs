using Hartonomous.Infrastructure.Repositories.EfCore.Models;

namespace Hartonomous.Infrastructure.Repositories.EfCore;

/// <summary>
/// Interface for autonomous learning operations
/// Replaces sp_Learn stored procedure
/// </summary>
public interface IAutonomousLearningRepository
{
    /// <summary>
    /// Learns from system performance and stores improvement history
    /// </summary>
    Task<LearningResult> LearnFromPerformanceAsync(
        PerformanceMetrics performanceMetrics,
        IReadOnlyList<ActionResult> improvementActions,
        CancellationToken cancellationToken = default);
}

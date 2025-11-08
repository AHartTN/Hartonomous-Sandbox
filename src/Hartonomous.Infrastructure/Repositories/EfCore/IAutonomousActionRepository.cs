using Hartonomous.Infrastructure.Repositories.EfCore.Models;

namespace Hartonomous.Infrastructure.Repositories.EfCore;

/// <summary>
/// Interface for autonomous action operations
/// Replaces sp_Act stored procedure
/// </summary>
public interface IAutonomousActionRepository
{
    /// <summary>
    /// Executes actions based on hypotheses received from analysis phase
    /// </summary>
    Task<ActionExecutionResult> ExecuteActionsAsync(
        Guid analysisId,
        IReadOnlyList<Hypothesis> hypotheses,
        int autoApproveThreshold = 3,
        CancellationToken cancellationToken = default);
}

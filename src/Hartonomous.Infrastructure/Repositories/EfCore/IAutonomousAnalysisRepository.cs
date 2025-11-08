using Hartonomous.Infrastructure.Repositories.EfCore.Models;

namespace Hartonomous.Infrastructure.Repositories.EfCore;

/// <summary>
/// Interface for autonomous analysis operations
/// Replaces sp_Analyze stored procedure
/// </summary>
public interface IAutonomousAnalysisRepository
{
    /// <summary>
    /// Performs system observation and analysis to detect anomalies and patterns
    /// </summary>
    Task<AnalysisResult> AnalyzeSystemAsync(
        int tenantId = 0,
        string analysisScope = "full",
        int lookbackHours = 24,
        CancellationToken cancellationToken = default);
}

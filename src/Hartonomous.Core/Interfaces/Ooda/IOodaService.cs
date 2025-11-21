using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces.Ooda;

/// <summary>
/// Service for OODA (Observe-Orient-Decide-Act) loop operations.
/// Enables autonomous self-optimization of the system.
/// </summary>
public interface IOodaService
{
    /// <summary>
    /// OODA Phase 1: Analyzes system performance and detects anomalies.
    /// Calls sp_Analyze stored procedure.
    /// </summary>
    Task<AnalysisResult> AnalyzeAsync(
        int tenantId,
        string analysisScope = "full",
        int lookbackHours = 24,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// OODA Phase 2: Generates hypotheses from observations.
    /// Calls sp_Hypothesize stored procedure.
    /// </summary>
    Task<HypothesisResult> HypothesizeAsync(
        Guid analysisId,
        string observationsJson,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// OODA Phase 3: Executes approved hypotheses/actions.
    /// Calls sp_Act stored procedure.
    /// </summary>
    Task<ActionResult> ActAsync(
        int tenantId,
        int autoApproveThreshold = 3,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts an autonomous prime number search job via the OODA loop.
    /// Calls sp_StartPrimeSearch stored procedure.
    /// </summary>
    Task<Guid> StartPrimeSearchAsync(
        long rangeStart,
        long rangeEnd,
        CancellationToken cancellationToken = default);
}

public record AnalysisResult(
    Guid AnalysisId,
    string Scope,
    int AnomaliesDetected,
    string ObservationsJson,
    DateTime AnalyzedAt);

public record HypothesisResult(
    Guid HypothesisId,
    Guid AnalysisId,
    int HypothesesGenerated,
    string HypothesesJson,
    DateTime GeneratedAt);

public record ActionResult(
    int ActionsExecuted,
    int ActionsSkipped,
    string ResultsJson,
    DateTime ExecutedAt);

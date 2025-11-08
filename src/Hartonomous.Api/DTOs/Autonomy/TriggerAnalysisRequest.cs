namespace Hartonomous.Api.DTOs.Autonomy;

public class TriggerAnalysisRequest
{
    /// <summary>Tenant ID for multi-tenant isolation</summary>
    public required int TenantId { get; init; }

    /// <summary>Analysis scope: 'full', 'models', 'embeddings', 'performance'</summary>
    public required string AnalysisScope { get; init; } = "full";

    /// <summary>Lookback window in hours for observation data</summary>
    public int LookbackHours { get; init; } = 24;
}

/// <summary>
/// Response from Analyze phase
/// </summary>

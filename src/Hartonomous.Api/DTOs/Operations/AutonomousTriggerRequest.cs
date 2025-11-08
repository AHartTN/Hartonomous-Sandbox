namespace Hartonomous.Api.DTOs.Operations;

/// <summary>
/// Request to trigger autonomous OODA loop cycle
/// </summary>
public class AutonomousTriggerRequest
{
    public required int TenantId { get; set; }

    public string AnalysisScope { get; set; } = "full"; // full, models, embeddings, performance

    public string Priority { get; set; } = "normal"; // high, normal, low
}

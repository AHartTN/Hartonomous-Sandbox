namespace Hartonomous.Infrastructure.Repositories.EfCore.Models;

/// <summary>
/// Represents a hypothesis generated during analysis for system improvement
/// </summary>
public class Hypothesis
{
    public Guid HypothesisId { get; set; }
    public string HypothesisType { get; set; } = string.Empty;
    public int Priority { get; set; }
    public double ConfidenceScore { get; set; }
    public string Description { get; set; } = string.Empty;
}

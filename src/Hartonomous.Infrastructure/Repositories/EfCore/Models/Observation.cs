namespace Hartonomous.Infrastructure.Repositories.EfCore.Models;

/// <summary>
/// Represents a system observation made during analysis
/// </summary>
public class Observation
{
    public Guid ObservationId { get; set; }
    public string ObservationType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ObservedAt { get; set; }
    public double Severity { get; set; }
}

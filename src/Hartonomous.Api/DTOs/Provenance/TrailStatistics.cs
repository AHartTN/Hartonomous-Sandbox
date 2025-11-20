namespace Hartonomous.Api.DTOs.Provenance;

public class TrailStatistics
{
    public int TotalEvents { get; set; }
    public int UniqueUsers { get; set; }
    public int VersionCount { get; set; }
    public int DaysSinceCreation { get; set; }
    public int AccessCount { get; set; }
}

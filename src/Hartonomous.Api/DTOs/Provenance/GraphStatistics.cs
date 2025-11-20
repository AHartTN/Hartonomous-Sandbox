namespace Hartonomous.Api.DTOs.Provenance;

public class GraphStatistics
{
    public int TotalNodes { get; set; }
    public int TotalRelationships { get; set; }
    public double AverageDegree { get; set; }
    public double Density { get; set; }
    public int Diameter { get; set; }
}

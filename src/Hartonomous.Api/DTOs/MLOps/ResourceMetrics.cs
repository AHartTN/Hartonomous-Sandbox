namespace Hartonomous.Api.DTOs.MLOps;

public class ResourceMetrics
{
    public int SqlServerCpuPercent { get; set; }
    public double SqlServerMemoryGb { get; set; }
    public int ClrMemoryMb { get; set; }
    public int SpatialIndexSizeMb { get; set; }
    public double Neo4jMemoryGb { get; set; }
}

namespace Hartonomous.Api.DTOs;

public class ModelStatsResponse
{
    public ModelStatsResponse(int totalModels, long totalParameters, int totalLayers, Dictionary<string, int>? architectureBreakdown)
    {
        TotalModels = totalModels;
        TotalParameters = totalParameters;
        TotalLayers = totalLayers;
        ArchitectureBreakdown = architectureBreakdown ?? new Dictionary<string, int>();
    }
    
    public int TotalModels { get; set; }
    public long TotalParameters { get; set; }
    public int TotalLayers { get; set; }
    public Dictionary<string, int> ArchitectureBreakdown { get; set; }
}

namespace Hartonomous.Api.DTOs;

public class ModelIngestResponse
{
    public ModelIngestResponse(int modelId, string modelName, string? architecture, long parameterCount, int layerCount)
    {
        ModelId = modelId;
        ModelName = modelName;
        Architecture = architecture;
        ParameterCount = parameterCount;
        LayerCount = layerCount;
    }
    
    public int ModelId { get; set; }
    public string ModelName { get; set; }
    public string? Architecture { get; set; }
    public long ParameterCount { get; set; }
    public int LayerCount { get; set; }
}

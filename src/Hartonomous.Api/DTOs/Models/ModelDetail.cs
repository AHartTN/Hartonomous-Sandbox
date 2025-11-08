namespace Hartonomous.Api.DTOs.Models;

public class ModelDetail
{
    public ModelDetail(int modelId, string modelName, string modelType, string? architecture, long? parameterCount,
        DateTime? ingestionDate, DateTime? lastUsed, int usageCount, string? config, ModelMetadataView? metadata, List<ModelLayerInfo> layers)
    {
        ModelId = modelId;
        ModelName = modelName;
        ModelType = modelType;
        Architecture = architecture;
        ParameterCount = parameterCount;
        IngestionDate = ingestionDate;
        LastUsed = lastUsed;
        UsageCount = usageCount;
        Config = config;
        Metadata = metadata;
        Layers = layers;
    }

    public int ModelId { get; set; }
    public string ModelName { get; set; }
    public string ModelType { get; set; }
    public string? Architecture { get; set; }
    public long? ParameterCount { get; set; }
    public DateTime? IngestionDate { get; set; }
    public DateTime? LastUsed { get; set; }
    public int UsageCount { get; set; }
    public string? Config { get; set; }
    public ModelMetadataView? Metadata { get; set; }
    public List<ModelLayerInfo> Layers { get; set; }
}

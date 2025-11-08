namespace Hartonomous.Api.DTOs.Models;

public class ModelSummary
{
    public ModelSummary(int modelId, string modelName, string modelType, string? architecture, long? parameterCount,
        DateTime? ingestionDate, DateTime? lastUsed, int usageCount)
    {
        ModelId = modelId;
        ModelName = modelName;
        ModelType = modelType;
        Architecture = architecture;
        ParameterCount = parameterCount;
        IngestionDate = ingestionDate;
        LastUsed = lastUsed;
        UsageCount = usageCount;
    }

    public int ModelId { get; set; }
    public string ModelName { get; set; }
    public string ModelType { get; set; }
    public string? Architecture { get; set; }
    public long? ParameterCount { get; set; }
    public DateTime? IngestionDate { get; set; }
    public DateTime? LastUsed { get; set; }
    public int UsageCount { get; set; }
    public int LayerCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

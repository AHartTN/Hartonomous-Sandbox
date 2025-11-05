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

public class LayerSummary
{
    public long LayerId { get; set; }
    public int LayerIdx { get; set; }
    public string? LayerName { get; set; }
    public string? LayerType { get; set; }
    public long? ParameterCount { get; set; }
}

public class DistillationRequest
{
    public required string StudentName { get; set; }
    public List<int>? LayerIndices { get; set; }
    public double ImportanceThreshold { get; set; } = 0.5;
}

public class DistillationResult
{
    public int StudentModelId { get; set; }
    public required string StudentName { get; set; }
    public int ParentModelId { get; set; }
    public long OriginalTensorAtoms { get; set; }
    public long StudentTensorAtoms { get; set; }
    public double CompressionRatio { get; set; }
    public double RetentionPercent { get; set; }
}

public class LayerDetail
{
    public long LayerId { get; set; }
    public int LayerIdx { get; set; }
    public string? LayerName { get; set; }
    public string? LayerType { get; set; }
    public long? ParameterCount { get; set; }
    public string? TensorShape { get; set; }
    public string? TensorDtype { get; set; }
    public double? CacheHitRate { get; set; }
    public double? AvgComputeTimeMs { get; set; }
    public int TensorAtomCount { get; set; }
    public double? AvgImportanceScore { get; set; }
}

public class ModelLayerInfo
{
    public ModelLayerInfo(long layerId, int layerIdx, string? layerName, string? layerType, long? parameterCount,
        string? tensorShape, string? tensorDtype, string? quantizationType, double? avgComputeTimeMs)
    {
        LayerId = layerId;
        LayerIdx = layerIdx;
        LayerName = layerName;
        LayerType = layerType;
        ParameterCount = parameterCount;
        TensorShape = tensorShape;
        TensorDtype = tensorDtype;
        QuantizationType = quantizationType;
        AvgComputeTimeMs = avgComputeTimeMs;
    }
    
    public long LayerId { get; set; }
    public int LayerIdx { get; set; }
    public string? LayerName { get; set; }
    public string? LayerType { get; set; }
    public long? ParameterCount { get; set; }
    public string? TensorShape { get; set; }
    public string? TensorDtype { get; set; }
    public string? QuantizationType { get; set; }
    public double? AvgComputeTimeMs { get; set; }
}

public class ModelMetadataView
{
    public ModelMetadataView(string? supportedTasks, string? supportedModalities, int? maxInputLength, int? maxOutputLength,
        int? embeddingDimension, string? performanceMetrics, string? trainingDataset, DateTime? trainingDate, string? license, string? sourceUrl)
    {
        SupportedTasks = supportedTasks;
        SupportedModalities = supportedModalities;
        MaxInputLength = maxInputLength;
        MaxOutputLength = maxOutputLength;
        EmbeddingDimension = embeddingDimension;
        PerformanceMetrics = performanceMetrics;
        TrainingDataset = trainingDataset;
        TrainingDate = trainingDate;
        License = license;
        SourceUrl = sourceUrl;
    }
    
    public string? SupportedTasks { get; set; }
    public string? SupportedModalities { get; set; }
    public int? MaxInputLength { get; set; }
    public int? MaxOutputLength { get; set; }
    public int? EmbeddingDimension { get; set; }
    public string? PerformanceMetrics { get; set; }
    public string? TrainingDataset { get; set; }
    public DateTime? TrainingDate { get; set; }
    public string? License { get; set; }
    public string? SourceUrl { get; set; }
}

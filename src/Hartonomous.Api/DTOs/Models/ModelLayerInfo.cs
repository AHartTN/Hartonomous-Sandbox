namespace Hartonomous.Api.DTOs.Models;

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

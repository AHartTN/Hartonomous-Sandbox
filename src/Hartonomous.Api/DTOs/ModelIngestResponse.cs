namespace Hartonomous.Api.DTOs;

/// <summary>
/// Response model for successful model ingestion.
/// </summary>
public record ModelIngestResponse(
    /// <summary>
    /// Unique identifier assigned to the ingested model.
    /// </summary>
    int ModelId,

    /// <summary>
    /// Name of the ingested model.
    /// </summary>
    string ModelName,

    /// <summary>
    /// Detected or specified architecture type.
    /// </summary>
    string Architecture,

    /// <summary>
    /// Total number of parameters in the model.
    /// </summary>
    long ParameterCount,

    /// <summary>
    /// Number of layers in the model.
    /// </summary>
    int LayerCount
);

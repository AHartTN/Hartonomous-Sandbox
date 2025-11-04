using System.Text.Json.Serialization;

namespace Hartonomous.Core.Enums;

/// <summary>
/// Represents the data modality (input/output type) supported by a model.
/// Uses [Flags] attribute for bitwise combinations (e.g., Text | Image for multimodal).
/// Maps to JSON strings in Model.Metadata.SupportedModalities using kebab-case convention.
/// </summary>
[Flags]
[JsonConverter(typeof(JsonStringEnumConverter<Modality>))]
public enum Modality
{
    /// <summary>
    /// No modality specified. Default value (required for flags enums).
    /// </summary>
    None = 0,

    /// <summary>
    /// Text data (natural language, documents).
    /// JSON value: "text"
    /// </summary>
    Text = 1 << 0,  // 1

    /// <summary>
    /// Image data (photos, renders, diagrams).
    /// JSON value: "image"
    /// </summary>
    Image = 1 << 1,  // 2

    /// <summary>
    /// Audio data (speech, music, sounds).
    /// JSON value: "audio"
    /// </summary>
    Audio = 1 << 2,  // 4

    /// <summary>
    /// Video data (frames, motion).
    /// JSON value: "video"
    /// </summary>
    Video = 1 << 3,  // 8

    /// <summary>
    /// Code data (T-SQL, C#, CLR functions). PRIMARY focus for Hartonomous.
    /// JSON value: "code"
    /// </summary>
    Code = 1 << 4,  // 16

    /// <summary>
    /// Tabular data (CSV, database tables, structured records).
    /// JSON value: "tabular"
    /// </summary>
    Tabular = 1 << 5,  // 32

    /// <summary>
    /// Time-series data (temporal sequences, metrics).
    /// JSON value: "time-series"
    /// </summary>
    TimeSeries = 1 << 6,  // 64

    /// <summary>
    /// Graph data (nodes, edges, relationships).
    /// JSON value: "graph"
    /// </summary>
    Graph = 1 << 7,  // 128

    /// <summary>
    /// Spatial/geometric data (coordinates, topologies).
    /// JSON value: "spatial"
    /// </summary>
    Spatial = 1 << 8,  // 256

    /// <summary>
    /// Vector embeddings (high-dimensional numerical representations).
    /// JSON value: "vector"
    /// </summary>
    Vector = 1 << 9  // 512
}

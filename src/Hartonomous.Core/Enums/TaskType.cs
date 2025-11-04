using System.Text.Json.Serialization;

namespace Hartonomous.Core.Enums;

/// <summary>
/// Represents the type of AI/ML task that can be performed by a model.
/// Maps to JSON strings in Model.Metadata.SupportedTasks using kebab-case convention.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<TaskType>))]
public enum TaskType
{
    /// <summary>
    /// Unknown or unspecified task type. Default value.
    /// </summary>
    None = 0,

    /// <summary>
    /// Text generation from prompts or context (e.g., GPT-style models).
    /// JSON value: "text-generation"
    /// </summary>
    TextGeneration = 1,

    /// <summary>
    /// Image generation from text prompts or other inputs.
    /// JSON value: "image-generation"
    /// </summary>
    ImageGeneration = 2,

    /// <summary>
    /// Audio generation from text or other modalities.
    /// JSON value: "audio-generation"
    /// </summary>
    AudioGeneration = 3,

    /// <summary>
    /// Video generation from prompts or frames.
    /// JSON value: "video-generation"
    /// </summary>
    VideoGeneration = 4,

    /// <summary>
    /// Code generation (T-SQL, C#, CLR functions). PRIMARY focus for Hartonomous.
    /// JSON value: "code-generation"
    /// </summary>
    CodeGeneration = 5,

    /// <summary>
    /// SQL query optimization and T-SQL performance tuning.
    /// JSON value: "sql-optimization"
    /// </summary>
    SqlOptimization = 6,

    /// <summary>
    /// Static code analysis, security scanning, complexity measurement.
    /// JSON value: "code-analysis"
    /// </summary>
    CodeAnalysis = 7,

    /// <summary>
    /// Automated test generation and execution.
    /// JSON value: "code-testing"
    /// </summary>
    CodeTesting = 8,

    /// <summary>
    /// Text embedding generation (vector representations).
    /// JSON value: "text-embedding"
    /// </summary>
    TextEmbedding = 9,

    /// <summary>
    /// Image embedding generation.
    /// JSON value: "image-embedding"
    /// </summary>
    ImageEmbedding = 10,

    /// <summary>
    /// Classification tasks (text, image, audio).
    /// JSON value: "classification"
    /// </summary>
    Classification = 11,

    /// <summary>
    /// Object detection in images or video frames.
    /// JSON value: "object-detection"
    /// </summary>
    ObjectDetection = 12,

    /// <summary>
    /// Semantic search and similarity matching.
    /// JSON value: "semantic-search"
    /// </summary>
    SemanticSearch = 13,

    /// <summary>
    /// Multimodal tasks combining text, image, audio, video.
    /// JSON value: "multimodal"
    /// </summary>
    Multimodal = 14
}

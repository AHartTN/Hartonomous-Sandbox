using System;
using System.Text.Json.Serialization;

namespace Hartonomous.Core.Enums;

/// <summary>
/// Represents the type of AI/ML task that can be performed by a model.
/// Maps to JSON strings in Model.Metadata.SupportedTasks using kebab-case convention.
/// Supports bitwise combinations for multimodal capabilities.
/// </summary>
[Flags]
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
    TextGeneration = 1 << 0, // 1

    /// <summary>
    /// Image generation from text prompts or other inputs.
    /// JSON value: "image-generation"
    /// </summary>
    ImageGeneration = 1 << 1, // 2

    /// <summary>
    /// Audio generation from text or other modalities.
    /// JSON value: "audio-generation"
    /// </summary>
    AudioGeneration = 1 << 2, // 4

    /// <summary>
    /// Video generation from prompts or frames.
    /// JSON value: "video-generation"
    /// </summary>
    VideoGeneration = 1 << 3, // 8

    /// <summary>
    /// Code generation (T-SQL, C#, CLR functions). PRIMARY focus for Hartonomous.
    /// JSON value: "code-generation"
    /// </summary>
    CodeGeneration = 1 << 4, // 16

    /// <summary>
    /// SQL query optimization and T-SQL performance tuning.
    /// JSON value: "sql-optimization"
    /// </summary>
    SqlOptimization = 1 << 5, // 32

    /// <summary>
    /// Static code analysis, security scanning, complexity measurement.
    /// JSON value: "code-analysis"
    /// </summary>
    CodeAnalysis = 1 << 6, // 64

    /// <summary>
    /// Automated test generation and execution.
    /// JSON value: "code-testing"
    /// </summary>
    CodeTesting = 1 << 7, // 128

    /// <summary>
    /// Text embedding generation (vector representations).
    /// JSON value: "text-embedding"
    /// </summary>
    TextEmbedding = 1 << 8, // 256

    /// <summary>
    /// Image embedding generation.
    /// JSON value: "image-embedding"
    /// </summary>
    ImageEmbedding = 1 << 9, // 512

    /// <summary>
    /// Classification tasks (text, image, audio).
    /// JSON value: "classification"
    /// </summary>
    Classification = 1 << 10, // 1024

    /// <summary>
    /// Object detection in images or video frames.
    /// JSON value: "object-detection"
    /// </summary>
    ObjectDetection = 1 << 11, // 2048

    /// <summary>
    /// Semantic search and similarity matching.
    /// JSON value: "semantic-search"
    /// </summary>
    SemanticSearch = 1 << 12, // 4096

    /// <summary>
    /// Multimodal tasks combining text, image, audio, video.
    /// JSON value: "multimodal"
    /// </summary>
    Multimodal = 1 << 13, // 8192

    // Common combinations for convenience
    /// <summary>
    /// All generation tasks combined.
    /// </summary>
    AllGeneration = TextGeneration | ImageGeneration | AudioGeneration | VideoGeneration | CodeGeneration,

    /// <summary>
    /// All embedding tasks combined.
    /// </summary>
    AllEmbedding = TextEmbedding | ImageEmbedding,

    /// <summary>
    /// All code-related tasks combined.
    /// </summary>
    AllCodeTasks = CodeGeneration | CodeAnalysis | CodeTesting | SqlOptimization,

    /// <summary>
    /// All vision-related tasks combined.
    /// </summary>
    AllVisionTasks = ImageGeneration | ImageEmbedding | ObjectDetection,

    /// <summary>
    /// All text-related tasks combined.
    /// </summary>
    AllTextTasks = TextGeneration | TextEmbedding | Classification | SemanticSearch
}

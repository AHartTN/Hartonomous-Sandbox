using Hartonomous.Core.Enums;

namespace Hartonomous.Core.Models;

/// <summary>
/// Represents the capabilities of an AI model derived from database metadata.
/// Type-safe alternative to bool flags using TaskType and Modality enums.
/// </summary>
public class ModelCapabilities
{
    /// <summary>
    /// All tasks supported by the model (parsed from Model.Metadata.SupportedTasks JSON).
    /// Array of TaskType enums mapped from kebab-case JSON strings.
    /// </summary>
    public TaskType[] SupportedTasks { get; init; } = Array.Empty<TaskType>();

    /// <summary>
    /// Combined modality flags supported by the model (parsed from Model.Metadata.SupportedModalities JSON).
    /// Uses bitwise flags for multimodal models (e.g., Text | Image | Code).
    /// </summary>
    public Modality SupportedModalities { get; init; } = Modality.Text;

    /// <summary>
    /// Primary modality for single-modality models. For multimodal, returns first flag.
    /// </summary>
    public Modality PrimaryModality => SupportedModalities == Modality.None 
        ? Modality.Text 
        : GetPrimaryModality(SupportedModalities);

    /// <summary>
    /// Maximum output tokens/length (from Model.Metadata.MaxOutputLength).
    /// </summary>
    public int MaxTokens { get; init; } = 2048;

    /// <summary>
    /// Maximum input context window (from Model.Metadata.MaxInputLength).
    /// </summary>
    public int MaxContextWindow { get; init; } = 4096;

    /// <summary>
    /// Embedding dimension if model supports embeddings (from Model.Metadata.EmbeddingDimension).
    /// </summary>
    public int? EmbeddingDimension { get; init; }

    /// <summary>
    /// Checks if model supports a specific task type.
    /// </summary>
    public bool SupportsTask(TaskType taskType) => SupportedTasks.Contains(taskType);

    /// <summary>
    /// Checks if model supports a specific modality (bitwise flag check).
    /// </summary>
    public bool SupportsModality(Modality modality) => (SupportedModalities & modality) != Modality.None;

    /// <summary>
    /// Checks if model supports ALL specified modalities (bitwise AND).
    /// </summary>
    public bool SupportsAllModalities(Modality modalities) => (SupportedModalities & modalities) == modalities;

    /// <summary>
    /// Checks if model supports ANY of the specified tasks.
    /// </summary>
    public bool SupportsAnyTask(params TaskType[] tasks) => tasks.Any(t => SupportedTasks.Contains(t));

    // Helper: Extract primary modality from combined flags
    private static Modality GetPrimaryModality(Modality modalities)
    {
        // Priority order: Code > Text > Image > Audio > Video > Other
        if (modalities.HasFlag(Modality.Code)) return Modality.Code;
        if (modalities.HasFlag(Modality.Text)) return Modality.Text;
        if (modalities.HasFlag(Modality.Image)) return Modality.Image;
        if (modalities.HasFlag(Modality.Audio)) return Modality.Audio;
        if (modalities.HasFlag(Modality.Video)) return Modality.Video;
        if (modalities.HasFlag(Modality.Vector)) return Modality.Vector;
        if (modalities.HasFlag(Modality.Spatial)) return Modality.Spatial;
        if (modalities.HasFlag(Modality.Graph)) return Modality.Graph;
        if (modalities.HasFlag(Modality.Tabular)) return Modality.Tabular;
        if (modalities.HasFlag(Modality.TimeSeries)) return Modality.TimeSeries;
        return Modality.Text;
    }
}

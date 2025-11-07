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
    /// Uses combined TaskType flags for O(1) capability checks instead of O(n) array searches.
    /// </summary>
    public TaskType SupportedTasks { get; init; } = TaskType.None;

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
    /// Checks if model supports a specific task type (O(1) bitwise flag check).
    /// </summary>
    public bool SupportsTask(TaskType taskType) => (SupportedTasks & taskType) != TaskType.None;

    /// <summary>
    /// Checks if model supports a specific modality (bitwise flag check).
    /// </summary>
    public bool SupportsModality(Modality modality) => (SupportedModalities & modality) != Modality.None;

    /// <summary>
    /// Checks if model supports ALL specified modalities (bitwise AND).
    /// </summary>
    public bool SupportsAllModalities(Modality modalities) => (SupportedModalities & modalities) == modalities;

    /// <summary>
    /// Checks if model supports ANY of the specified tasks (bitwise OR check).
    /// </summary>
    public bool SupportsAnyTask(params TaskType[] tasks)
    {
        var combinedTasks = TaskType.None;
        foreach (var task in tasks)
            combinedTasks |= task;
        return (SupportedTasks & combinedTasks) != TaskType.None;
    }

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

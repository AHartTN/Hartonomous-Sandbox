using System.Text.Json;

namespace Hartonomous.Core.Enums;

/// <summary>
/// Extension methods for enum conversions between C# enums and JSON kebab-case strings.
/// Provides bidirectional mapping for Model.Metadata JSON fields.
/// </summary>
public static class EnumExtensions
{
    private static readonly JsonSerializerOptions KebabCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower
    };

    #region TaskType Conversions

    /// <summary>
    /// Converts a kebab-case JSON string to TaskType enum.
    /// Case-insensitive. Returns TaskType.None for unrecognized values.
    /// PERFORMANCE: Uses ReadOnlySpan<char> for zero-allocation string processing.
    /// </summary>
    /// <param name="taskString">JSON task string (e.g., "code-generation", "text-generation")</param>
    /// <returns>Corresponding TaskType enum value</returns>
    public static TaskType ToTaskType(this string? taskString)
    {
        if (string.IsNullOrWhiteSpace(taskString))
            return TaskType.None;

        // PERFORMANCE: Use ReadOnlySpan<char> for zero-allocation processing
        return ToTaskType(taskString.AsSpan());
    }

    /// <summary>
    /// Converts a ReadOnlySpan<char> to TaskType enum (zero-allocation).
    /// </summary>
    public static TaskType ToTaskType(this ReadOnlySpan<char> taskSpan)
    {
        if (taskSpan.IsEmpty || taskSpan.IsWhiteSpace())
            return TaskType.None;

        // Create normalized span (lowercase, replace underscores)
        Span<char> normalized = stackalloc char[taskSpan.Length];
        taskSpan.ToLowerInvariant(normalized);

        // Replace underscores with hyphens in-place
        for (int i = 0; i < normalized.Length; i++)
        {
            if (normalized[i] == '_')
                normalized[i] = '-';
        }

        var normalizedSpan = normalized.Trim();

        // PERFORMANCE: Use span-based comparisons for better performance
        if (normalizedSpan.SequenceEqual("text-generation".AsSpan()))
            return TaskType.TextGeneration;
        if (normalizedSpan.SequenceEqual("image-generation".AsSpan()))
            return TaskType.ImageGeneration;
        if (normalizedSpan.SequenceEqual("audio-generation".AsSpan()))
            return TaskType.AudioGeneration;
        if (normalizedSpan.SequenceEqual("video-generation".AsSpan()))
            return TaskType.VideoGeneration;
        if (normalizedSpan.SequenceEqual("code-generation".AsSpan()))
            return TaskType.CodeGeneration;
        if (normalizedSpan.SequenceEqual("sql-optimization".AsSpan()))
            return TaskType.SqlOptimization;
        if (normalizedSpan.SequenceEqual("code-analysis".AsSpan()))
            return TaskType.CodeAnalysis;
        if (normalizedSpan.SequenceEqual("code-testing".AsSpan()))
            return TaskType.CodeTesting;
        if (normalizedSpan.SequenceEqual("text-embedding".AsSpan()))
            return TaskType.TextEmbedding;
        if (normalizedSpan.SequenceEqual("image-embedding".AsSpan()))
            return TaskType.ImageEmbedding;
        if (normalizedSpan.SequenceEqual("classification".AsSpan()))
            return TaskType.Classification;
        if (normalizedSpan.SequenceEqual("object-detection".AsSpan()))
            return TaskType.ObjectDetection;
        if (normalizedSpan.SequenceEqual("semantic-search".AsSpan()))
            return TaskType.SemanticSearch;
        if (normalizedSpan.SequenceEqual("multimodal".AsSpan()))
            return TaskType.Multimodal;

        return TaskType.None;
    }

    /// <summary>
    /// Converts TaskType enum to kebab-case JSON string.
    /// </summary>
    /// <param name="taskType">TaskType enum value</param>
    /// <returns>Kebab-case string (e.g., "code-generation")</returns>
    public static string ToJsonString(this TaskType taskType)
    {
        return taskType switch
        {
            TaskType.TextGeneration => "text-generation",
            TaskType.ImageGeneration => "image-generation",
            TaskType.AudioGeneration => "audio-generation",
            TaskType.VideoGeneration => "video-generation",
            TaskType.CodeGeneration => "code-generation",
            TaskType.SqlOptimization => "sql-optimization",
            TaskType.CodeAnalysis => "code-analysis",
            TaskType.CodeTesting => "code-testing",
            TaskType.TextEmbedding => "text-embedding",
            TaskType.ImageEmbedding => "image-embedding",
            TaskType.Classification => "classification",
            TaskType.ObjectDetection => "object-detection",
            TaskType.SemanticSearch => "semantic-search",
            TaskType.Multimodal => "multimodal",
            TaskType.None => "none",
            _ => "unknown"
        };
    }

    /// <summary>
    /// Parses a JSON array of task strings into combined TaskType flags.
    /// PERFORMANCE: Returns combined flags for O(1) capability checks.
    /// </summary>
    /// <param name="jsonArray">JSON string like ["text-generation", "code-generation"]</param>
    /// <returns>Combined TaskType flags</returns>
    public static TaskType ParseTaskTypes(string? jsonArray)
    {
        if (string.IsNullOrWhiteSpace(jsonArray))
            return TaskType.None;

        try
        {
            var taskStrings = JsonSerializer.Deserialize<string[]>(jsonArray);
            if (taskStrings == null || taskStrings.Length == 0)
                return TaskType.None;

            var result = TaskType.None;
            foreach (var taskString in taskStrings)
            {
                var taskType = taskString.ToTaskType();
                if (taskType != TaskType.None)
                    result |= taskType;  // Bitwise OR to combine flags
            }
            return result;
        }
        catch
        {
            return TaskType.None;
        }
    }

    #endregion

    #region Modality Conversions

    /// <summary>
    /// Converts a kebab-case JSON string to Modality enum flag.
    /// Case-insensitive. Returns Modality.None for unrecognized values.
    /// PERFORMANCE: Uses ReadOnlySpan<char> for zero-allocation string processing.
    /// </summary>
    /// <param name="modalityString">JSON modality string (e.g., "code", "text", "image")</param>
    /// <returns>Corresponding Modality enum value</returns>
    public static Modality ToModality(this string? modalityString)
    {
        if (string.IsNullOrWhiteSpace(modalityString))
            return Modality.None;

        // PERFORMANCE: Use ReadOnlySpan<char> for zero-allocation processing
        return ToModality(modalityString.AsSpan());
    }

    /// <summary>
    /// Converts a ReadOnlySpan<char> to Modality enum flag (zero-allocation).
    /// </summary>
    public static Modality ToModality(this ReadOnlySpan<char> modalitySpan)
    {
        if (modalitySpan.IsEmpty || modalitySpan.IsWhiteSpace())
            return Modality.None;

        // PERFORMANCE: Normalize to lowercase on stack
        Span<char> normalized = stackalloc char[modalitySpan.Length];
        modalitySpan.ToLowerInvariant(normalized);
        var normalizedSpan = normalized.Trim();

        // PERFORMANCE: Use span-based comparisons
        if (normalizedSpan.SequenceEqual("text".AsSpan()))
            return Modality.Text;
        if (normalizedSpan.SequenceEqual("image".AsSpan()))
            return Modality.Image;
        if (normalizedSpan.SequenceEqual("audio".AsSpan()))
            return Modality.Audio;
        if (normalizedSpan.SequenceEqual("video".AsSpan()))
            return Modality.Video;
        if (normalizedSpan.SequenceEqual("code".AsSpan()))
            return Modality.Code;
        if (normalizedSpan.SequenceEqual("tabular".AsSpan()))
            return Modality.Tabular;
        if (normalizedSpan.SequenceEqual("time-series".AsSpan()) || normalizedSpan.SequenceEqual("timeseries".AsSpan()))
            return Modality.TimeSeries;
        if (normalizedSpan.SequenceEqual("graph".AsSpan()))
            return Modality.Graph;
        if (normalizedSpan.SequenceEqual("spatial".AsSpan()))
            return Modality.Spatial;
        if (normalizedSpan.SequenceEqual("vector".AsSpan()))
            return Modality.Vector;

        return Modality.None;
    }

    /// <summary>
    /// Converts Modality enum flag to kebab-case JSON string.
    /// For combined flags, returns the primary modality.
    /// </summary>
    /// <param name="modality">Modality enum value</param>
    /// <returns>Kebab-case string (e.g., "code", "text")</returns>
    public static string ToJsonString(this Modality modality)
    {
        // Handle single flags
        if (modality == Modality.None) return "none";
        if (modality == Modality.Text) return "text";
        if (modality == Modality.Image) return "image";
        if (modality == Modality.Audio) return "audio";
        if (modality == Modality.Video) return "video";
        if (modality == Modality.Code) return "code";
        if (modality == Modality.Tabular) return "tabular";
        if (modality == Modality.TimeSeries) return "time-series";
        if (modality == Modality.Graph) return "graph";
        if (modality == Modality.Spatial) return "spatial";
        if (modality == Modality.Vector) return "vector";

        // For combined flags, return first set bit (primary modality)
        if (modality.HasFlag(Modality.Text)) return "text";
        if (modality.HasFlag(Modality.Code)) return "code";
        if (modality.HasFlag(Modality.Image)) return "image";
        if (modality.HasFlag(Modality.Audio)) return "audio";
        if (modality.HasFlag(Modality.Video)) return "video";

        return "unknown";
    }

    /// <summary>
    /// Parses a JSON array of modality strings into combined Modality flags.
    /// </summary>
    /// <param name="jsonArray">JSON string like ["text", "code", "image"]</param>
    /// <returns>Combined Modality flags</returns>
    public static Modality ParseModalities(string? jsonArray)
    {
        if (string.IsNullOrWhiteSpace(jsonArray))
            return Modality.None;

        try
        {
            var modalityStrings = JsonSerializer.Deserialize<string[]>(jsonArray);
            if (modalityStrings == null || modalityStrings.Length == 0)
                return Modality.None;

            var result = Modality.None;
            foreach (var modalityString in modalityStrings)
            {
                var modality = modalityString.ToModality();
                if (modality != Modality.None)
                    result |= modality;  // Bitwise OR to combine flags
            }
            return result;
        }
        catch
        {
            return Modality.None;
        }
    }

    /// <summary>
    /// Converts combined Modality flags to JSON string array.
    /// </summary>
    /// <param name="modality">Combined Modality flags</param>
    /// <returns>JSON array string like ["text", "code", "image"]</returns>
    public static string ToJsonArray(this Modality modality)
    {
        var modalities = new List<string>();

        if (modality.HasFlag(Modality.Text)) modalities.Add("text");
        if (modality.HasFlag(Modality.Image)) modalities.Add("image");
        if (modality.HasFlag(Modality.Audio)) modalities.Add("audio");
        if (modality.HasFlag(Modality.Video)) modalities.Add("video");
        if (modality.HasFlag(Modality.Code)) modalities.Add("code");
        if (modality.HasFlag(Modality.Tabular)) modalities.Add("tabular");
        if (modality.HasFlag(Modality.TimeSeries)) modalities.Add("time-series");
        if (modality.HasFlag(Modality.Graph)) modalities.Add("graph");
        if (modality.HasFlag(Modality.Spatial)) modalities.Add("spatial");
        if (modality.HasFlag(Modality.Vector)) modalities.Add("vector");

        return JsonSerializer.Serialize(modalities);
    }

    #endregion

    #region EnsembleStrategy Conversions

    /// <summary>
    /// Converts a kebab-case JSON string to EnsembleStrategy enum.
    /// Case-insensitive. Returns EnsembleStrategy.None for unrecognized values.
    /// </summary>
    public static EnsembleStrategy ToEnsembleStrategy(this string? strategyString)
    {
        if (string.IsNullOrWhiteSpace(strategyString))
            return EnsembleStrategy.None;

        if (Enum.TryParse<EnsembleStrategy>(strategyString, ignoreCase: true, out var directResult))
            return directResult;

        return strategyString.ToLowerInvariant() switch
        {
            "weighted-voting" or "voting" => EnsembleStrategy.WeightedVoting,
            "stacking" => EnsembleStrategy.Stacking,
            "routing" => EnsembleStrategy.Routing,
            "averaging" or "average" => EnsembleStrategy.Averaging,
            "max-pooling" or "maxpooling" => EnsembleStrategy.MaxPooling,
            "caruana-selection" or "caruana" => EnsembleStrategy.CaruanaSelection,
            "distillation" or "distill" => EnsembleStrategy.Distillation,
            _ => EnsembleStrategy.None
        };
    }

    /// <summary>
    /// Converts EnsembleStrategy enum to kebab-case JSON string.
    /// </summary>
    public static string ToJsonString(this EnsembleStrategy strategy)
    {
        return strategy switch
        {
            EnsembleStrategy.WeightedVoting => "weighted-voting",
            EnsembleStrategy.Stacking => "stacking",
            EnsembleStrategy.Routing => "routing",
            EnsembleStrategy.Averaging => "averaging",
            EnsembleStrategy.MaxPooling => "max-pooling",
            EnsembleStrategy.CaruanaSelection => "caruana-selection",
            EnsembleStrategy.Distillation => "distillation",
            EnsembleStrategy.None => "none",
            _ => "unknown"
        };
    }

    #endregion

    #region ReasoningMode Conversions

    /// <summary>
    /// Converts a kebab-case JSON string to ReasoningMode enum.
    /// Case-insensitive. Returns ReasoningMode.Direct for unrecognized values.
    /// </summary>
    public static ReasoningMode ToReasoningMode(this string? reasoningString)
    {
        if (string.IsNullOrWhiteSpace(reasoningString))
            return ReasoningMode.Direct;

        if (Enum.TryParse<ReasoningMode>(reasoningString, ignoreCase: true, out var directResult))
            return directResult;

        return reasoningString.ToLowerInvariant() switch
        {
            "direct" => ReasoningMode.Direct,
            "analytical" => ReasoningMode.Analytical,
            "creative" => ReasoningMode.Creative,
            "chain-of-thought" or "cot" => ReasoningMode.ChainOfThought,
            "tree-of-thought" or "tot" => ReasoningMode.TreeOfThought,
            "self-consistency" => ReasoningMode.SelfConsistency,
            "reflexion" => ReasoningMode.Reflexion,
            _ => ReasoningMode.Direct
        };
    }

    /// <summary>
    /// Converts ReasoningMode enum to kebab-case JSON string.
    /// </summary>
    public static string ToJsonString(this ReasoningMode reasoning)
    {
        return reasoning switch
        {
            ReasoningMode.Direct => "direct",
            ReasoningMode.Analytical => "analytical",
            ReasoningMode.Creative => "creative",
            ReasoningMode.ChainOfThought => "chain-of-thought",
            ReasoningMode.TreeOfThought => "tree-of-thought",
            ReasoningMode.SelfConsistency => "self-consistency",
            ReasoningMode.Reflexion => "reflexion",
            _ => "direct"
        };
    }

    #endregion

    #region Validation Helpers

    /// <summary>
    /// Validates if an enum value is defined (not out of range).
    /// </summary>
    public static bool IsValid<TEnum>(this TEnum enumValue) where TEnum : struct, Enum
    {
        return Enum.IsDefined(typeof(TEnum), enumValue);
    }

    /// <summary>
    /// Checks if any of the specified flags are set in the combined value.
    /// </summary>
    public static bool HasAnyFlag(this Modality value, Modality flags)
    {
        return (value & flags) != Modality.None;
    }

    /// <summary>
    /// Checks if all of the specified flags are set in the combined value.
    /// </summary>
    public static bool HasAllFlags(this Modality value, Modality flags)
    {
        return (value & flags) == flags;
    }

    #endregion
}

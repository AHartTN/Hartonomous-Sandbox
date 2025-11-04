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
    /// </summary>
    /// <param name="taskString">JSON task string (e.g., "code-generation", "text-generation")</param>
    /// <returns>Corresponding TaskType enum value</returns>
    public static TaskType ToTaskType(this string? taskString)
    {
        if (string.IsNullOrWhiteSpace(taskString))
            return TaskType.None;

        // Try exact match first (fast path)
        if (Enum.TryParse<TaskType>(taskString, ignoreCase: true, out var directResult))
            return directResult;

        // Handle kebab-case conversions
        return taskString.ToLowerInvariant() switch
        {
            "text-generation" => TaskType.TextGeneration,
            "image-generation" => TaskType.ImageGeneration,
            "audio-generation" => TaskType.AudioGeneration,
            "video-generation" => TaskType.VideoGeneration,
            "code-generation" => TaskType.CodeGeneration,
            "sql-optimization" => TaskType.SqlOptimization,
            "code-analysis" => TaskType.CodeAnalysis,
            "code-testing" => TaskType.CodeTesting,
            "text-embedding" => TaskType.TextEmbedding,
            "image-embedding" => TaskType.ImageEmbedding,
            "classification" => TaskType.Classification,
            "object-detection" => TaskType.ObjectDetection,
            "semantic-search" => TaskType.SemanticSearch,
            "multimodal" => TaskType.Multimodal,
            _ => TaskType.None
        };
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
    /// Parses a JSON array of task strings into TaskType array.
    /// </summary>
    /// <param name="jsonArray">JSON string like ["text-generation", "code-generation"]</param>
    /// <returns>Array of TaskType enums</returns>
    public static TaskType[] ParseTaskTypes(string? jsonArray)
    {
        if (string.IsNullOrWhiteSpace(jsonArray))
            return Array.Empty<TaskType>();

        try
        {
            var taskStrings = JsonSerializer.Deserialize<string[]>(jsonArray);
            return taskStrings?.Select(s => s.ToTaskType()).Where(t => t != TaskType.None).ToArray()
                ?? Array.Empty<TaskType>();
        }
        catch
        {
            return Array.Empty<TaskType>();
        }
    }

    #endregion

    #region Modality Conversions

    /// <summary>
    /// Converts a kebab-case JSON string to Modality enum flag.
    /// Case-insensitive. Returns Modality.None for unrecognized values.
    /// </summary>
    /// <param name="modalityString">JSON modality string (e.g., "code", "text", "image")</param>
    /// <returns>Corresponding Modality enum value</returns>
    public static Modality ToModality(this string? modalityString)
    {
        if (string.IsNullOrWhiteSpace(modalityString))
            return Modality.None;

        // Try exact match first (fast path)
        if (Enum.TryParse<Modality>(modalityString, ignoreCase: true, out var directResult))
            return directResult;

        // Handle kebab-case and variations
        return modalityString.ToLowerInvariant() switch
        {
            "text" => Modality.Text,
            "image" => Modality.Image,
            "audio" => Modality.Audio,
            "video" => Modality.Video,
            "code" => Modality.Code,
            "tabular" => Modality.Tabular,
            "time-series" or "timeseries" => Modality.TimeSeries,
            "graph" => Modality.Graph,
            "spatial" => Modality.Spatial,
            "vector" => Modality.Vector,
            _ => Modality.None
        };
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

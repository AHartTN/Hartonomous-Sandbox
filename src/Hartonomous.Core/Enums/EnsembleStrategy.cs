using System.Text.Json.Serialization;

namespace Hartonomous.Core.Enums;

/// <summary>
/// Defines how multiple models should be combined in ensemble inference.
/// Maps to InferenceRequests.EnsembleStrategy column and SQL procedure strategies.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<EnsembleStrategy>))]
public enum EnsembleStrategy
{
    /// <summary>
    /// No ensemble strategy specified (single model execution).
    /// </summary>
    None = 0,

    /// <summary>
    /// Weighted voting: Combine predictions using model weights, majority rules.
    /// For classification: Most common class.
    /// For generation: Most frequent output or highest consensus score.
    /// JSON value: "weighted-voting"
    /// </summary>
    WeightedVoting = 1,

    /// <summary>
    /// Stacking: Combine rich outputs from multiple models into a single comprehensive response.
    /// Meta-model combines individual model outputs (e.g., merge T-SQL from 3 generators).
    /// JSON value: "stacking"
    /// </summary>
    Stacking = 2,

    /// <summary>
    /// Routing: Select the single best model for the task based on capabilities and performance.
    /// No combination, just intelligent model selection.
    /// JSON value: "routing"
    /// </summary>
    Routing = 3,

    /// <summary>
    /// Averaging: Simple average of numerical outputs (for regression, embeddings).
    /// JSON value: "averaging"
    /// </summary>
    Averaging = 4,

    /// <summary>
    /// Max pooling: Take maximum value from each dimension across model outputs.
    /// JSON value: "max-pooling"
    /// </summary>
    MaxPooling = 5,

    /// <summary>
    /// Caruana ensemble selection: Iteratively add models that improve overall score.
    /// JSON value: "caruana-selection"
    /// </summary>
    CaruanaSelection = 6,

    /// <summary>
    /// Distillation: Train smaller model from multiple teacher model outputs.
    /// JSON value: "distillation"
    /// </summary>
    Distillation = 7
}

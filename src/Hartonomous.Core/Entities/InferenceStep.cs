namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a detailed breakdown of a single step in a multi-step inference operation.
/// </summary>
public class InferenceStep
{
    /// <summary>
    /// Gets or sets the unique identifier for the inference step.
    /// </summary>
    public long StepId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the parent inference request.
    /// </summary>
    public long InferenceId { get; set; }

    /// <summary>
    /// Gets or sets the sequential step number within the inference request.
    /// </summary>
    public int StepNumber { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the model used in this step.
    /// </summary>
    public int? ModelId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the specific layer executed in this step.
    /// </summary>
    public long? LayerId { get; set; }

    /// <summary>
    /// Gets or sets the type of operation performed (e.g., 'vector_search', 'attention', 'feedforward').
    /// </summary>
    public string? OperationType { get; set; }

    /// <summary>
    /// Gets or sets the query text or SQL executed in this step.
    /// </summary>
    public string? QueryText { get; set; }

    /// <summary>
    /// Gets or sets the name of the database index used for this step.
    /// </summary>
    public string? IndexUsed { get; set; }

    /// <summary>
    /// Gets or sets the number of rows examined during this step.
    /// </summary>
    public long? RowsExamined { get; set; }

    /// <summary>
    /// Gets or sets the number of rows returned by this step.
    /// </summary>
    public long? RowsReturned { get; set; }

    /// <summary>
    /// Gets or sets the duration of this step in milliseconds.
    /// </summary>
    public int? DurationMs { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether cached data was used in this step.
    /// </summary>
    public bool CacheUsed { get; set; } = false;

    /// <summary>
    /// Gets or sets the parent inference request.
    /// </summary>
    public InferenceRequest InferenceRequest { get; set; } = null!;

    /// <summary>
    /// Gets or sets the model used in this step.
    /// </summary>
    public Model? Model { get; set; }
}

using Hartonomous.Data.Entities;

namespace Hartonomous.Core.Pipelines.Inference;

/// <summary>
/// Strongly-typed data models for Ensemble Inference pipeline steps.
/// Enterprise-grade DTOs replacing tuples for better maintainability and type safety.
/// </summary>

/// <summary>
/// Output from candidate retrieval step.
/// Contains request and retrieved candidate atoms.
/// </summary>
public sealed record CandidateRetrievalResult
{
    public required EnsembleInferenceRequest Request { get; init; }
    public required List<Atom> Candidates { get; init; }
}

/// <summary>
/// Contribution from a single model in the ensemble.
/// </summary>
public sealed record ModelContribution
{
    public required int ModelId { get; init; }
    public required string ModelName { get; init; }
    public required string Output { get; init; }
    public double Weight { get; init; } = 1.0;
    public double Confidence { get; init; }
    public TimeSpan Duration { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Output from ensemble model invocation step.
/// Contains all model contributions for aggregation.
/// </summary>
public sealed record EnsembleInvocationResult
{
    public required EnsembleInferenceRequest Request { get; init; }
    public required List<Atom> Candidates { get; init; }
    public required List<ModelContribution> Contributions { get; init; }
}

/// <summary>
/// Output from result aggregation step.
/// Contains final aggregated result from ensemble voting.
/// </summary>
public sealed record AggregationResult
{
    public required EnsembleInferenceRequest Request { get; init; }
    public required List<Atom> Candidates { get; init; }
    public required List<ModelContribution> Contributions { get; init; }
    public required string FinalOutput { get; init; }
    public double Confidence { get; init; }
}

/// <summary>
/// Final pipeline output from ensemble inference.
/// </summary>
public sealed record EnsembleInferenceResult
{
    public required EnsembleInferenceRequest Request { get; init; }
    public required string Output { get; init; }
    public double Confidence { get; init; }
    public required List<ModelContribution> Contributions { get; init; }
    public required List<Atom> CandidateAtoms { get; init; }
    public long? InferenceRequestId { get; init; }
    public TimeSpan TotalDuration { get; init; }
    public string? CorrelationId { get; init; }
    public string AggregationStrategy { get; init; } = "MajorityVoting";
}

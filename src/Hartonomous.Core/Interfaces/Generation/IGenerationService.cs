using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Interfaces.Generation;

/// <summary>
/// Service for text and multi-modal generation operations.
/// </summary>
public interface IGenerationService
{
    /// <summary>
    /// Generates text via LLM.
    /// Calls sp_GenerateText stored procedure.
    /// </summary>
    Task<GenerationResult> GenerateTextAsync(
        string prompt,
        int maxTokens = 100,
        float temperature = 1.0f,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates text by iteratively finding next atoms in spatial embedding space.
    /// Calls sp_GenerateTextSpatial stored procedure.
    /// </summary>
    Task<GenerationResult> GenerateTextSpatialAsync(
        string prompt,
        int maxTokens = 10,
        float temperature = 1.0f,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attention-based text generation with multi-head attention.
    /// Calls sp_GenerateWithAttention stored procedure.
    /// </summary>
    Task<long> GenerateWithAttentionAsync(
        int modelId,
        string inputAtomIds,
        string contextJson = "{}",
        int maxTokens = 100,
        float temperature = 1.0f,
        int topK = 50,
        float topP = 0.9f,
        int attentionHeads = 8,
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// A* pathfinding through semantic space from start to target concept.
    /// Calls sp_GenerateOptimalPath stored procedure.
    /// </summary>
    Task<IEnumerable<PathStep>> GenerateOptimalPathAsync(
        long startAtomId,
        int targetConceptId,
        int maxSteps = 50,
        float neighborRadius = 0.5f,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Predicts next token via spatial nearest-neighbor search.
    /// Calls sp_SpatialNextToken stored procedure.
    /// </summary>
    Task<IEnumerable<TokenPrediction>> PredictNextTokenAsync(
        long currentAtomId,
        Geometry spatialDirection,
        int topK = 10,
        CancellationToken cancellationToken = default);
}

public record GenerationResult(
    string GeneratedText,
    int TokensGenerated,
    float? AverageConfidence);

public record PathStep(
    int StepIndex,
    long AtomId,
    float CumulativeCost,
    float HeuristicCost);

public record TokenPrediction(
    long AtomId,
    string? TokenText,
    float Probability);

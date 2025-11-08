using System.Data;
using System.Globalization;
using System.Text.Json;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.ValueObjects;
using Hartonomous.Core.Utilities;
using Hartonomous.Infrastructure.Data.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services.Inference;

/// <summary>
/// Implements text generation using spatial search for context retrieval and SQL Server stored procedures.
/// Generates text by finding relevant context through vector similarity, then invoking generation procedures.
/// </summary>
public sealed class TextGenerationService : ITextGenerationService
{
    private readonly IAtomEmbeddingRepository _atomEmbeddings;
    private readonly ISqlCommandExecutor _sql;
    private readonly ILogger<TextGenerationService> _logger;

    /// <summary>
    /// Initializes a new text generation service.
    /// </summary>
    /// <param name="atomEmbeddingRepository">Repository for hybrid search operations.</param>
    /// <param name="sqlCommandExecutor">SQL command executor abstraction.</param>
    /// <param name="logger">Structured logger for diagnostics.</param>
    public TextGenerationService(
        IAtomEmbeddingRepository atomEmbeddingRepository,
        ISqlCommandExecutor sqlCommandExecutor,
        ILogger<TextGenerationService> logger)
    {
        _atomEmbeddings = atomEmbeddingRepository ?? throw new ArgumentNullException(nameof(atomEmbeddingRepository));
        _sql = sqlCommandExecutor ?? throw new ArgumentNullException(nameof(sqlCommandExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<GenerationResult> GenerateViaSpatialAsync(
        float[] promptEmbedding,
        int maxTokens = 50,
        float temperature = 0.7f,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating text via spatial search, maxTokens={MaxTokens}, temperature={Temperature}",
            maxTokens,
            temperature);

        if (promptEmbedding is null || promptEmbedding.Length == 0)
        {
            throw new ArgumentException("Prompt embedding must contain at least one value", nameof(promptEmbedding));
        }

        // Step 1: Prepare vector and compute spatial projection
        var padded = VectorUtility.PadToSqlLength(promptEmbedding, out _);
        var sqlVector = padded.ToSqlVector();
        var spatialPoint = await _atomEmbeddings
            .ComputeSpatialProjectionAsync(sqlVector, promptEmbedding.Length, cancellationToken)
            .ConfigureAwait(false);

        // Step 2: Use hybrid search to recover representative context terms for the prompt
        var nearestContext = await _atomEmbeddings
            .HybridSearchAsync(promptEmbedding, spatialPoint, 32, 8, cancellationToken)
            .ConfigureAwait(false);

        var promptTokens = nearestContext
            .Select(result => result.Embedding?.Atom?.CanonicalText)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Select(text => text!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();

        if (promptTokens.Count == 0)
        {
            // Fall back to a numeric fingerprint so the stored procedure receives a non-empty prompt
            var fallback = string.Join(
                ' ',
                promptEmbedding
                    .Take(Math.Min(16, promptEmbedding.Length))
                    .Select(value => value.ToString("0.###", CultureInfo.InvariantCulture)));

            promptTokens.Add(string.IsNullOrWhiteSpace(fallback) ? "[vector-context]" : fallback);
        }

        var promptText = string.Join(' ', promptTokens);

        // Step 3: Execute sp_GenerateText with prepared context
        return await _sql.ExecuteAsync(async (command, token) =>
        {
            command.CommandText = "dbo.sp_GenerateText";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@prompt", promptText));
            command.Parameters.Add(new SqlParameter("@max_tokens", maxTokens));
            command.Parameters.Add(new SqlParameter("@temperature", temperature));
            command.Parameters.Add(new SqlParameter("@ModelIds", DBNull.Value));
            command.Parameters.Add(new SqlParameter("@top_k", Math.Clamp(maxTokens, 1, 12)));

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            if (!await reader.ReadAsync(token).ConfigureAwait(false))
            {
                _logger.LogWarning("Stored procedure sp_GenerateText returned no rows");
                return new GenerationResult
                {
                    GeneratedText = string.Empty,
                    TokenIds = [],
                    TokenConfidences = [],
                    TokenCount = 0,
                    AverageConfidence = 0.0f,
                    InferenceId = 0
                };
            }

            var infIdOrd = reader.GetOrdinal("InferenceId");
            var textOrd = reader.GetOrdinal("GeneratedText");
            var countOrd = reader.GetOrdinal("TokensGenerated");
            var detailsOrd = reader.GetOrdinal("TokenDetails");

            var inferenceId = reader.GetInt64(infIdOrd);
            var generatedText = reader.GetStringOrNull(textOrd) ?? string.Empty;
            var tokenCount = reader.GetInt32OrNull(countOrd) ?? 0;
            var tokenDetailsJson = reader.GetStringOrNull(detailsOrd);

            var tokenDetails = new List<(int? TokenId, float Score)>();

            if (!string.IsNullOrWhiteSpace(tokenDetailsJson))
            {
                try
                {
                    using var json = JsonDocument.Parse(tokenDetailsJson);
                    foreach (var element in json.RootElement.EnumerateArray())
                    {
                        int? tokenId = null;
                        if (element.TryGetProperty("AtomId", out var atomIdProperty) &&
                            atomIdProperty.ValueKind == JsonValueKind.Number)
                        {
                            var atomId = atomIdProperty.GetInt64();
                            if (atomId <= int.MaxValue)
                            {
                                tokenId = (int)atomId;
                            }
                        }

                        float scoreValue = 0f;
                        if (element.TryGetProperty("Score", out var scoreProperty) &&
                            scoreProperty.ValueKind == JsonValueKind.Number &&
                            scoreProperty.TryGetDouble(out var scoreDouble))
                        {
                            scoreValue = (float)scoreDouble;
                        }
                        else if (element.TryGetProperty("Distance", out var distanceProperty) &&
                            distanceProperty.ValueKind == JsonValueKind.Number)
                        {
                            var distance = (float)distanceProperty.GetDouble();
                            scoreValue = distance == 0 ? 1.0f : 1.0f / (1.0f + distance);
                        }

                        tokenDetails.Add((tokenId, scoreValue));
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse token details JSON from sp_GenerateText");
                }
            }

            var tokenConfidencesAll = NormalizeScores(tokenDetails.Select(detail => detail.Score).ToList());

            var tokenIds = new List<int>();
            var tokenConfidences = new List<float>();
            for (var i = 0; i < tokenDetails.Count; i++)
            {
                var detail = tokenDetails[i];
                if (!detail.TokenId.HasValue)
                {
                    continue;
                }

                tokenIds.Add(detail.TokenId.Value);
                tokenConfidences.Add(i < tokenConfidencesAll.Count ? tokenConfidencesAll[i] : 0f);
            }

            var finalTokenCount = tokenCount > 0 ? tokenCount : tokenIds.Count;
            var averageConfidence = tokenConfidences.Count > 0
                ? tokenConfidences.Average()
                : 0f;

            return new GenerationResult
            {
                GeneratedText = generatedText,
                TokenIds = tokenIds,
                TokenConfidences = tokenConfidences,
                TokenCount = finalTokenCount,
                AverageConfidence = averageConfidence,
                InferenceId = inferenceId
            };
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Normalizes raw scores using softmax (exponential normalization).
    /// Converts distance/similarity scores into probability distribution.
    /// </summary>
    private static IReadOnlyList<float> NormalizeScores(IReadOnlyList<float> rawScores)
    {
        if (rawScores.Count == 0)
        {
            return [];
        }

        var maxScore = rawScores.Max();
        var expScores = new double[rawScores.Count];
        double sum = 0;
        for (var i = 0; i < rawScores.Count; i++)
        {
            var value = Math.Exp(rawScores[i] - maxScore);
            expScores[i] = value;
            sum += value;
        }

        if (sum <= double.Epsilon)
        {
            return rawScores.Select(_ => 0f).ToArray();
        }

        var confidences = new float[rawScores.Count];
        for (var i = 0; i < rawScores.Count; i++)
        {
            confidences[i] = (float)(expScores[i] / sum);
        }

        return confidences;
    }
}

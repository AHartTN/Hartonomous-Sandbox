using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Utilities;
using Hartonomous.Infrastructure.Data.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlTypes;
using System.Data;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Provides spatially-aware inference operations backed by SQL Server stored procedures.
/// </summary>
public class SpatialInferenceService : ISpatialInferenceService
{
    private readonly ISqlCommandExecutor _sqlCommandExecutor;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpatialInferenceService"/> class.
    /// </summary>
    /// <param name="sqlCommandExecutor">Centralized SQL command executor abstraction.</param>
    public SpatialInferenceService(ISqlCommandExecutor sqlCommandExecutor)
    {
        _sqlCommandExecutor = sqlCommandExecutor ?? throw new ArgumentNullException(nameof(sqlCommandExecutor));
    }

    /// <summary>
    /// Executes the spatial attention stored procedure to retrieve nearby token relationships.
    /// </summary>
    /// <param name="queryTokenId">Identifier of the anchor token.</param>
    /// <param name="contextSize">Number of neighboring tokens to return.</param>
    /// <param name="cancellationToken">Token that cancels the database call.</param>
    /// <returns>Ordered list of attention results with spatial metadata.</returns>
    public async Task<IReadOnlyList<SpatialAttentionResult>> SpatialAttentionAsync(
        long queryTokenId,
        int contextSize,
        CancellationToken cancellationToken = default)
    {
        return await _sqlCommandExecutor.ExecuteAsync(async (command, token) =>
        {
            command.CommandText = "dbo.sp_SpatialAttention";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@QueryAtomId", SqlDbType.BigInt) { Value = queryTokenId });
            command.Parameters.Add(new SqlParameter("@ContextSize", SqlDbType.Int) { Value = contextSize });

            var results = new List<SpatialAttentionResult>();
            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                var tokenId = reader.GetInt64(reader.GetOrdinal("TokenId"));
                var tokenText = reader.GetStringOrNull(reader.GetOrdinal("TokenText")) ?? string.Empty;
                var spatialDistance = reader.GetDouble(reader.GetOrdinal("SpatialDistance"));
                var attentionWeight = reader.GetDouble(reader.GetOrdinal("AttentionWeight"));
                var resolution = reader.GetStringOrNull(reader.GetOrdinal("ResolutionLevel")) ?? "UNKNOWN";

                results.Add(new SpatialAttentionResult(tokenId, tokenText, attentionWeight, spatialDistance, resolution));
            }

            return (IReadOnlyList<SpatialAttentionResult>)results;
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Predicts likely next tokens using spatial context and probabilistic smoothing.
    /// </summary>
    /// <param name="contextTokenIds">Sequence of token identifiers representing the current context.</param>
    /// <param name="temperature">Temperature parameter applied to the probability distribution.</param>
    /// <param name="topK">Maximum number of predictions to return.</param>
    /// <param name="cancellationToken">Token that cancels the database call.</param>
    /// <returns>Sorted collection of candidate tokens with probabilities.</returns>
    public async Task<IReadOnlyList<SpatialNextTokenPrediction>> PredictNextTokenAsync(
        IEnumerable<long> contextTokenIds,
        double temperature,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var contextCsv = string.Join(',', contextTokenIds);

        return await _sqlCommandExecutor.ExecuteAsync(async (command, token) =>
        {
            command.CommandText = "dbo.sp_SpatialNextToken";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@context_atom_ids", SqlDbType.NVarChar, -1) { Value = contextCsv });
            command.Parameters.Add(new SqlParameter("@temperature", SqlDbType.Float) { Value = temperature });
            command.Parameters.Add(new SqlParameter("@top_k", SqlDbType.Int) { Value = topK });

            var results = new List<SpatialNextTokenPrediction>();
            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                var tokenId = reader.GetInt64(reader.GetOrdinal("TokenId"));
                var tokenText = reader.GetStringOrNull(reader.GetOrdinal("TokenText")) ?? string.Empty;
                var spatialDistance = reader.GetDouble(reader.GetOrdinal("SpatialDistance"));
                var probabilityScore = reader.GetDouble(reader.GetOrdinal("ProbabilityScore"));

                results.Add(new SpatialNextTokenPrediction(tokenId, tokenText, probabilityScore, spatialDistance));
            }

            return (IReadOnlyList<SpatialNextTokenPrediction>)results;
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs a multi-resolution spatial search combining coarse and fine projections.
    /// </summary>
    /// <param name="queryX">X coordinate of the query point.</param>
    /// <param name="queryY">Y coordinate of the query point.</param>
    /// <param name="queryZ">Z coordinate of the query point.</param>
    /// <param name="coarseCandidates">Number of coarse candidates to evaluate.</param>
    /// <param name="fineCandidates">Number of fine-grained candidates to evaluate.</param>
    /// <param name="topK">Maximum number of final results to return.</param>
    /// <param name="cancellationToken">Token that cancels the database call.</param>
    /// <returns>Collection of search results with spatial and metadata details.</returns>
    public async Task<IReadOnlyList<MultiResolutionSearchResult>> MultiResolutionSearchAsync(
        double queryX,
        double queryY,
        double queryZ,
        int coarseCandidates,
        int fineCandidates,
        int topK,
        CancellationToken cancellationToken = default)
    {
        return await _sqlCommandExecutor.ExecuteAsync(async (command, token) =>
        {
            command.CommandText = "dbo.sp_MultiResolutionSearch";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@query_x", SqlDbType.Float) { Value = queryX });
            command.Parameters.Add(new SqlParameter("@query_y", SqlDbType.Float) { Value = queryY });
            command.Parameters.Add(new SqlParameter("@query_z", SqlDbType.Float) { Value = queryZ });
            command.Parameters.Add(new SqlParameter("@coarse_candidates", SqlDbType.Int) { Value = coarseCandidates });
            command.Parameters.Add(new SqlParameter("@fine_candidates", SqlDbType.Int) { Value = fineCandidates });
            command.Parameters.Add(new SqlParameter("@final_top_k", SqlDbType.Int) { Value = topK });

            var results = new List<MultiResolutionSearchResult>();
            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                var embeddingId = reader.GetInt64(reader.GetOrdinal("AtomEmbeddingId"));
                var atomId = reader.GetInt64(reader.GetOrdinal("AtomId"));
                var modality = reader.GetString(reader.GetOrdinal("Modality"));
                var subtype = reader.GetStringOrNull(reader.GetOrdinal("Subtype"));
                var sourceType = reader.GetStringOrNull(reader.GetOrdinal("SourceType"));
                var sourceUri = reader.GetStringOrNull(reader.GetOrdinal("SourceUri"));
                var canonicalText = reader.GetStringOrNull(reader.GetOrdinal("CanonicalText"));
                var embeddingType = reader.GetString(reader.GetOrdinal("EmbeddingType"));
                int? modelId = reader.GetInt32OrNull(reader.GetOrdinal("ModelId"));
                var spatialDistance = reader.GetDouble(reader.GetOrdinal("SpatialDistance"));
                var coarseDistance = reader.GetDoubleOrNull(reader.GetOrdinal("CoarseDistance")) ?? double.NaN;

                results.Add(new MultiResolutionSearchResult(
                    embeddingId,
                    atomId,
                    modality,
                    subtype,
                    sourceType,
                    sourceUri,
                    canonicalText,
                    embeddingType,
                    modelId,
                    spatialDistance,
                    coarseDistance));
            }

            return (IReadOnlyList<MultiResolutionSearchResult>)results;
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes cognitive activation to retrieve embeddings exceeding an activation threshold.
    /// </summary>
    /// <param name="queryVector">Embedding used as the activation query.</param>
    /// <param name="activationThreshold">Minimum activation strength required.</param>
    /// <param name="maxActivated">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Token that cancels the database call.</param>
    /// <returns>List of activated embeddings with contextual metadata.</returns>
    public async Task<IReadOnlyList<CognitiveActivationResult>> CognitiveActivationAsync(
        float[] queryVector,
        double activationThreshold,
        int maxActivated,
        CancellationToken cancellationToken = default)
    {
        return await _sqlCommandExecutor.ExecuteAsync(async (command, token) =>
        {
            command.CommandText = "dbo.sp_CognitiveActivation";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@query_embedding", queryVector.ToSqlVector()));
            command.Parameters.Add(new SqlParameter("@activation_threshold", SqlDbType.Float) { Value = activationThreshold });
            command.Parameters.Add(new SqlParameter("@max_activated", SqlDbType.Int) { Value = maxActivated });

            var results = new List<CognitiveActivationResult>();
            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                var embeddingId = reader.GetInt64(reader.GetOrdinal("AtomEmbeddingId"));
                var atomId = reader.GetInt64(reader.GetOrdinal("AtomId"));
                var modality = reader.GetStringOrNull(reader.GetOrdinal("Modality"));
                var subtype = reader.GetStringOrNull(reader.GetOrdinal("Subtype"));
                var sourceType = reader.GetStringOrNull(reader.GetOrdinal("SourceType"));
                var canonicalText = reader.GetStringOrNull(reader.GetOrdinal("CanonicalText"));
                var strength = reader.GetDouble(reader.GetOrdinal("ActivationStrength"));
                var level = reader.GetString(reader.GetOrdinal("ActivationLevel"));

                results.Add(new CognitiveActivationResult(embeddingId, atomId, strength, level, modality, subtype, sourceType, canonicalText));
            }

            return (IReadOnlyList<CognitiveActivationResult>)results;
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Generates text via the spatial generation stored procedure.
    /// </summary>
    /// <param name="prompt">Prompt text provided to the spatial generator.</param>
    /// <param name="maxTokens">Maximum number of tokens the procedure may generate.</param>
    /// <param name="temperature">Sampling temperature controlling randomness.</param>
    /// <param name="cancellationToken">Token that cancels the generation call.</param>
    /// <returns>Generated text; falls back to the prompt when no output is produced.</returns>
    public async Task<string> GenerateTextSpatialAsync(
        string prompt,
        int maxTokens,
        double temperature,
        CancellationToken cancellationToken = default)
    {
        return await _sqlCommandExecutor.ExecuteAsync(async (command, token) =>
        {
            command.CommandText = "dbo.sp_GenerateTextSpatial";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@prompt", SqlDbType.NVarChar, -1) { Value = prompt });
            command.Parameters.Add(new SqlParameter("@max_tokens", SqlDbType.Int) { Value = maxTokens });
            command.Parameters.Add(new SqlParameter("@temperature", SqlDbType.Float) { Value = temperature });

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            if (!await reader.ReadAsync(token).ConfigureAwait(false))
            {
                return prompt;
            }

            return reader.GetStringOrNull(reader.GetOrdinal("GeneratedText")) ?? prompt;
        }, cancellationToken).ConfigureAwait(false);
    }
}

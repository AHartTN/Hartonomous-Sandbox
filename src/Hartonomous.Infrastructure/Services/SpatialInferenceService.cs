using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Provides spatially-aware inference operations backed by SQL Server stored procedures.
/// </summary>
public class SpatialInferenceService : ISpatialInferenceService
{
    private readonly HartonomousDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpatialInferenceService"/> class.
    /// </summary>
    /// <param name="context">Database context used to execute spatial inference procedures.</param>
    public SpatialInferenceService(HartonomousDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
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
        await using var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "dbo.sp_SpatialAttention";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(new SqlParameter("@query_token_id", queryTokenId));
        command.Parameters.Add(new SqlParameter("@context_size", contextSize));

        var results = new List<SpatialAttentionResult>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var tokenId = reader.GetInt32(reader.GetOrdinal("token_id"));
            var tokenText = reader.IsDBNull(reader.GetOrdinal("token_text"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("token_text"));
            var spatialDistance = reader.GetDouble(reader.GetOrdinal("spatial_distance"));
            var attentionWeight = reader.GetDouble(reader.GetOrdinal("attention_weight"));
            var resolution = reader.IsDBNull(reader.GetOrdinal("resolution_level"))
                ? "UNKNOWN"
                : reader.GetString(reader.GetOrdinal("resolution_level"));

            results.Add(new SpatialAttentionResult(tokenId, tokenText, attentionWeight, spatialDistance, resolution));
        }

        return results;
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

        await using var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "dbo.sp_SpatialNextToken";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(new SqlParameter("@context_tokens", contextCsv));
        command.Parameters.Add(new SqlParameter("@temperature", temperature));

        var results = new List<SpatialNextTokenPrediction>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var tokenId = reader.GetInt32(reader.GetOrdinal("token_id"));
            var tokenText = reader.IsDBNull(reader.GetOrdinal("token_text"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("token_text"));
            var distance = reader.GetDouble(reader.GetOrdinal("distance"));
            var probability = reader.GetDouble(reader.GetOrdinal("probability_score"));

            results.Add(new SpatialNextTokenPrediction(tokenId, tokenText, probability, distance));
        }

        return results
            .OrderByDescending(r => r.ProbabilityScore)
            .Take(topK)
            .ToList();
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
        await using var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "dbo.sp_MultiResolutionSearch";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(new SqlParameter("@query_x", queryX));
        command.Parameters.Add(new SqlParameter("@query_y", queryY));
        command.Parameters.Add(new SqlParameter("@query_z", queryZ));
        command.Parameters.Add(new SqlParameter("@coarse_candidates", coarseCandidates));
        command.Parameters.Add(new SqlParameter("@fine_candidates", fineCandidates));
        command.Parameters.Add(new SqlParameter("@final_top_k", topK));

        var results = new List<MultiResolutionSearchResult>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var embeddingId = reader.GetInt64(reader.GetOrdinal("AtomEmbeddingId"));
            var atomId = reader.GetInt64(reader.GetOrdinal("AtomId"));
            var modality = reader.GetString(reader.GetOrdinal("Modality"));
            var subtype = reader.IsDBNull(reader.GetOrdinal("Subtype")) ? null : reader.GetString(reader.GetOrdinal("Subtype"));
            var sourceType = reader.IsDBNull(reader.GetOrdinal("SourceType")) ? null : reader.GetString(reader.GetOrdinal("SourceType"));
            var sourceUri = reader.IsDBNull(reader.GetOrdinal("SourceUri")) ? null : reader.GetString(reader.GetOrdinal("SourceUri"));
            var canonicalText = reader.IsDBNull(reader.GetOrdinal("CanonicalText")) ? null : reader.GetString(reader.GetOrdinal("CanonicalText"));
            var embeddingType = reader.GetString(reader.GetOrdinal("EmbeddingType"));
            int? modelId = reader.IsDBNull(reader.GetOrdinal("ModelId")) ? null : reader.GetInt32(reader.GetOrdinal("ModelId"));
            var spatialDistance = reader.GetDouble(reader.GetOrdinal("SpatialDistance"));
            var coarseDistance = reader.IsDBNull(reader.GetOrdinal("CoarseDistance")) ? double.NaN : reader.GetDouble(reader.GetOrdinal("CoarseDistance"));

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

        return results;
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
        await using var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "dbo.sp_CognitiveActivation";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(new SqlParameter("@query_embedding", new SqlVector<float>(queryVector)));
        command.Parameters.Add(new SqlParameter("@activation_threshold", activationThreshold));
        command.Parameters.Add(new SqlParameter("@max_activated", maxActivated));

        var results = new List<CognitiveActivationResult>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var embeddingId = reader.GetInt64(reader.GetOrdinal("AtomEmbeddingId"));
            var atomId = reader.GetInt64(reader.GetOrdinal("AtomId"));
            var modality = reader.IsDBNull(reader.GetOrdinal("Modality")) ? null : reader.GetString(reader.GetOrdinal("Modality"));
            var subtype = reader.IsDBNull(reader.GetOrdinal("Subtype")) ? null : reader.GetString(reader.GetOrdinal("Subtype"));
            var sourceType = reader.IsDBNull(reader.GetOrdinal("SourceType")) ? null : reader.GetString(reader.GetOrdinal("SourceType"));
            var canonicalText = reader.IsDBNull(reader.GetOrdinal("CanonicalText")) ? null : reader.GetString(reader.GetOrdinal("CanonicalText"));
            var strength = reader.GetDouble(reader.GetOrdinal("ActivationStrength"));
            var level = reader.GetString(reader.GetOrdinal("ActivationLevel"));

            results.Add(new CognitiveActivationResult(embeddingId, atomId, strength, level, modality, subtype, sourceType, canonicalText));
        }

        return results;
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
        await using var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "dbo.sp_GenerateTextSpatial";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(new SqlParameter("@prompt", prompt));
        command.Parameters.Add(new SqlParameter("@max_tokens", maxTokens));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return prompt;
        }

        return reader.IsDBNull(reader.GetOrdinal("generated_text"))
            ? prompt
            : reader.GetString(reader.GetOrdinal("generated_text"));
    }
}

using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Hartonomous.Infrastructure.Services;

public class SpatialInferenceService : ISpatialInferenceService
{
    private readonly HartonomousDbContext _context;

    public SpatialInferenceService(HartonomousDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

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

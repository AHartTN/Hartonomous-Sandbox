using Azure.Core;
using Azure.Identity;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces.Semantic;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services.Semantic;

/// <summary>
/// SQL Server implementation of semantic analysis operations.
/// Provides feature extraction, duplicate detection, and similarity computation.
/// </summary>
public sealed class SqlSemanticService : ISemanticService
{
    private readonly string _connectionString;
    private readonly TokenCredential _credential;
    private readonly ILogger<SqlSemanticService> _logger;

    public SqlSemanticService(
        ILogger<SqlSemanticService> logger,
        IOptions<DatabaseOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var databaseOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _connectionString = databaseOptions.HartonomousDb;
        _credential = new DefaultAzureCredential();
    }

    public async Task ComputeFeaturesAsync(
        long atomEmbeddingId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(atomEmbeddingId);

        _logger.LogInformation("ComputeFeatures: AtomEmbeddingId {AtomEmbeddingId}", atomEmbeddingId);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_ComputeSemanticFeatures", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@atomEmbeddingId", atomEmbeddingId);

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("ComputeFeatures completed for AtomEmbeddingId {AtomEmbeddingId}", atomEmbeddingId);
    }

    public async Task ComputeAllFeaturesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ComputeAllFeatures: Starting batch feature extraction");

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_ComputeAllSemanticFeatures", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 1800 // 30 minutes - batch operation
        };

        var processedParam = new SqlParameter("@embeddingsProcessed", SqlDbType.Int)
        { Direction = ParameterDirection.Output };
        command.Parameters.Add(processedParam);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var processed = (int)(processedParam.Value ?? 0);

        _logger.LogInformation("ComputeAllFeatures completed: {ProcessedCount} embeddings processed", processed);
    }

    public async Task<IEnumerable<DuplicateResult>> DetectDuplicatesAsync(
        float threshold = 0.95f,
        int batchSize = 1000,
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        _logger.LogInformation("DetectDuplicates: Threshold {Threshold}, BatchSize {BatchSize}",
            threshold, batchSize);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_DetectDuplicates", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 300 // 5 minutes
        };

        command.Parameters.AddWithValue("@threshold", threshold);
        command.Parameters.AddWithValue("@batchSize", batchSize);
        command.Parameters.AddWithValue("@tenantId", tenantId);

        var duplicates = new List<DuplicateResult>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            duplicates.Add(new DuplicateResult(
                reader.GetInt64(reader.GetOrdinal("Atom1Id")),
                reader.GetInt64(reader.GetOrdinal("Atom2Id")),
                (float)reader.GetDouble(reader.GetOrdinal("SimilarityScore")),
                reader.GetString(reader.GetOrdinal("Atom1Text")),
                reader.GetString(reader.GetOrdinal("Atom2Text"))));
        }

        _logger.LogInformation("DetectDuplicates completed: {DuplicateCount} pairs found", duplicates.Count);

        return duplicates;
    }

    public async Task<float> ComputeSimilarityAsync(
        long atom1Id,
        long atom2Id,
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(atom1Id);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(atom2Id);

        _logger.LogInformation("ComputeSimilarity: Atom1 {Atom1}, Atom2 {Atom2}", atom1Id, atom2Id);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_SemanticSimilarity", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 30
        };

        command.Parameters.AddWithValue("@atom1Id", atom1Id);
        command.Parameters.AddWithValue("@atom2Id", atom2Id);
        command.Parameters.AddWithValue("@tenantId", tenantId);

        var similarityParam = new SqlParameter("@similarity", SqlDbType.Float)
        { Direction = ParameterDirection.Output };
        command.Parameters.Add(similarityParam);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var similarity = (float)(double)(similarityParam.Value ?? 0.0);

        _logger.LogInformation("ComputeSimilarity completed: {Similarity:F3}", similarity);

        return similarity;
    }

    public async Task ExtractMetadataAsync(
        long atomId,
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(atomId);

        _logger.LogInformation("ExtractMetadata: AtomId {AtomId}, TenantId {TenantId}", atomId, tenantId);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_ExtractMetadata", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@AtomId", atomId);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("ExtractMetadata completed for AtomId {AtomId}", atomId);
    }

    private async Task SetupConnectionAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        // Use managed identity if no password in connection string
        if (!_connectionString.Contains("Password=", StringComparison.OrdinalIgnoreCase) &&
            !_connectionString.Contains("Integrated Security=true", StringComparison.OrdinalIgnoreCase))
        {
            var tokenRequestContext = new TokenRequestContext(["https://database.windows.net/.default"]);
            var token = await _credential.GetTokenAsync(tokenRequestContext, cancellationToken);
            connection.AccessToken = token.Token;
        }

        await connection.OpenAsync(cancellationToken);
    }
}

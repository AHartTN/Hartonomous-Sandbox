using Azure.Core;
using Azure.Identity;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces.Generation;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services.Generation;

/// <summary>
/// SQL Server implementation of generation operations.
/// </summary>
public sealed class SqlGenerationService : IGenerationService
{
    private readonly string _connectionString;
    private readonly TokenCredential _credential;
    private readonly ILogger<SqlGenerationService> _logger;

    public SqlGenerationService(
        ILogger<SqlGenerationService> logger,
        IOptions<DatabaseOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var databaseOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _connectionString = databaseOptions.HartonomousDb;
        _credential = new DefaultAzureCredential();
    }

    public async Task<GenerationResult> GenerateTextAsync(
        string prompt,
        int maxTokens = 100,
        float temperature = 1.0f,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GenerateText: MaxTokens {MaxTokens}, Temperature {Temperature}", maxTokens, temperature);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_GenerateText", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

        command.Parameters.AddWithValue("@prompt", prompt);
        command.Parameters.AddWithValue("@max_tokens", maxTokens);
        command.Parameters.AddWithValue("@temperature", temperature);

        var outputParam = new SqlParameter("@GeneratedText", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Output };
        command.Parameters.Add(outputParam);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return new GenerationResult(
            outputParam.Value?.ToString() ?? "",
            maxTokens,
            null);
    }

    public async Task<GenerationResult> GenerateTextSpatialAsync(
        string prompt,
        int maxTokens = 10,
        float temperature = 1.0f,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_GenerateTextSpatial", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

        command.Parameters.AddWithValue("@prompt", prompt);
        command.Parameters.AddWithValue("@max_tokens", maxTokens);
        command.Parameters.AddWithValue("@temperature", temperature);

        var outputParam = new SqlParameter("@GeneratedText", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Output };
        command.Parameters.Add(outputParam);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return new GenerationResult(outputParam.Value?.ToString() ?? "", maxTokens, null);
    }

    public async Task<long> GenerateWithAttentionAsync(
        int modelId,
        string inputAtomIds,
        string contextJson = "{}",
        int maxTokens = 100,
        float temperature = 1.0f,
        int topK = 50,
        float topP = 0.9f,
        int attentionHeads = 8,
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_GenerateWithAttention", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 180
        };

        command.Parameters.AddWithValue("@ModelId", modelId);
        command.Parameters.AddWithValue("@InputAtomIds", inputAtomIds);
        command.Parameters.AddWithValue("@ContextJson", contextJson);
        command.Parameters.AddWithValue("@MaxTokens", maxTokens);
        command.Parameters.AddWithValue("@Temperature", temperature);
        command.Parameters.AddWithValue("@TopK", topK);
        command.Parameters.AddWithValue("@TopP", topP);
        command.Parameters.AddWithValue("@AttentionHeads", attentionHeads);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        var resultParam = new SqlParameter("@GeneratedAtomId", SqlDbType.BigInt) { Direction = ParameterDirection.Output };
        command.Parameters.Add(resultParam);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return (long)(resultParam.Value ?? 0L);
    }

    public async Task<IEnumerable<PathStep>> GenerateOptimalPathAsync(
        long startAtomId,
        int targetConceptId,
        int maxSteps = 50,
        float neighborRadius = 0.5f,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_GenerateOptimalPath", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@StartAtomId", startAtomId);
        command.Parameters.AddWithValue("@TargetConceptId", targetConceptId);
        command.Parameters.AddWithValue("@MaxSteps", maxSteps);
        command.Parameters.AddWithValue("@NeighborRadius", neighborRadius);

        var results = new List<PathStep>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new PathStep(
                reader.GetInt32(reader.GetOrdinal("StepIndex")),
                reader.GetInt64(reader.GetOrdinal("AtomId")),
                (float)reader.GetDouble(reader.GetOrdinal("CumulativeCost")),
                (float)reader.GetDouble(reader.GetOrdinal("HeuristicCost"))));
        }

        return results;
    }

    public async Task<IEnumerable<TokenPrediction>> PredictNextTokenAsync(
        long currentAtomId,
        Geometry spatialDirection,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_SpatialNextToken", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 30
        };

        command.Parameters.AddWithValue("@CurrentAtomId", currentAtomId);
        command.Parameters.AddWithValue("@SpatialDirection", new WKTWriter().Write(spatialDirection));
        command.Parameters.AddWithValue("@TopK", topK);

        var results = new List<TokenPrediction>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new TokenPrediction(
                reader.GetInt64(reader.GetOrdinal("AtomId")),
                reader.IsDBNull(reader.GetOrdinal("TokenText")) ? null : reader.GetString(reader.GetOrdinal("TokenText")),
                (float)reader.GetDouble(reader.GetOrdinal("Probability"))));
        }

        return results;
    }

    private async Task SetupConnectionAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
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

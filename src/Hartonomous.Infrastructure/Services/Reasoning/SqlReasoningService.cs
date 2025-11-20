using Azure.Core;
using Azure.Identity;
using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces.Reasoning;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Data;

namespace Hartonomous.Infrastructure.Services.Reasoning;

/// <summary>
/// Production SQL Server implementation of reasoning services using Arc-enabled managed identity authentication.
/// Inherits from ReasoningServiceBase for standardized validation and telemetry.
/// </summary>
public sealed class SqlReasoningService : ReasoningServiceBase<SqlReasoningService>
{
    private readonly string _connectionString;
    private readonly TokenCredential _credential;

    public SqlReasoningService(
        ILogger<SqlReasoningService> logger,
        IOptions<DatabaseOptions> options)
        : base(logger)
    {
        var databaseOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _connectionString = databaseOptions.HartonomousDb;
        _credential = new DefaultAzureCredential();
    }

    protected override async Task<ReasoningResult> ExecuteChainOfThoughtInternalAsync(
        long sessionId,
        string prompt,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object> { { "operation", "ChainOfThought" } };
        var resultJson = await ExecuteReasoningOperationAsync("ChainOfThought", prompt, parameters, cancellationToken);
        
        var result = JsonConvert.DeserializeObject<ReasoningResult>(resultJson) 
            ?? new ReasoningResult 
            { 
                Strategy = "ChainOfThought", 
                Prompt = prompt, 
                Conclusion = "No result returned",
                SessionId = sessionId,
                ExecutedAt = DateTime.UtcNow
            };

        return result;
    }

    protected override async Task<ReasoningResult> ExecuteTreeOfThoughtInternalAsync(
        long sessionId,
        string prompt,
        int maxBranches,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object> 
        { 
            { "operation", "TreeOfThought" },
            { "maxBranches", maxBranches }
        };
        var resultJson = await ExecuteReasoningOperationAsync("TreeOfThought", prompt, parameters, cancellationToken);
        
        var result = JsonConvert.DeserializeObject<ReasoningResult>(resultJson)
            ?? new ReasoningResult
            {
                Strategy = "TreeOfThought",
                Prompt = prompt,
                Conclusion = "No result returned",
                SessionId = sessionId,
                ExecutedAt = DateTime.UtcNow
            };

        return result;
    }

    protected override async Task<IEnumerable<ReasoningResult>> GetSessionHistoryInternalAsync(
        long sessionId,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);

        var tokenRequestContext = new TokenRequestContext(["https://database.windows.net/.default"]);
        var token = await _credential.GetTokenAsync(tokenRequestContext, cancellationToken);
        connection.AccessToken = token.Token;

        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "SELECT * FROM ReasoningResults WHERE SessionId = @sessionId ORDER BY ExecutedAt", 
            connection)
        {
            CommandTimeout = 30
        };

        command.Parameters.Add(new SqlParameter("@sessionId", sessionId));

        var results = new List<ReasoningResult>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ReasoningResult
            {
                Id = reader.GetInt64(reader.GetOrdinal("Id")),
                SessionId = reader.GetInt64(reader.GetOrdinal("SessionId")),
                Strategy = reader.GetString(reader.GetOrdinal("Strategy")),
                Prompt = reader.GetString(reader.GetOrdinal("Prompt")),
                Conclusion = reader.GetString(reader.GetOrdinal("Conclusion")),
                IntermediateSteps = reader.IsDBNull(reader.GetOrdinal("IntermediateSteps")) 
                    ? null 
                    : reader.GetString(reader.GetOrdinal("IntermediateSteps")),
                ConfidenceScore = reader.GetDouble(reader.GetOrdinal("ConfidenceScore")),
                ExecutedAt = reader.GetDateTime(reader.GetOrdinal("ExecutedAt")),
                ExecutionTimeMs = reader.GetInt64(reader.GetOrdinal("ExecutionTimeMs"))
            });
        }

        return results;
    }

    private async Task<string> ExecuteReasoningOperationAsync(
        string operation,
        string input,
        Dictionary<string, object>? parameters,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);

        // Use Arc-enabled managed identity authentication
        var tokenRequestContext = new TokenRequestContext(["https://database.windows.net/.default"]);
        var token = await _credential.GetTokenAsync(tokenRequestContext, cancellationToken);
        connection.AccessToken = token.Token;

        await connection.OpenAsync(cancellationToken);

        var storedProcedure = operation switch
        {
            "ChainOfThought" => "dbo.sp_RunInference",
            "TreeOfThought" => "dbo.sp_RunInference", // Same SP, different parameters
            _ => throw new ArgumentException($"Unknown reasoning operation: {operation}", nameof(operation))
        };

        await using var command = new SqlCommand(storedProcedure, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 300 // 5 minutes for complex reasoning
        };

        // Add input parameter
        command.Parameters.Add(new SqlParameter("@input", SqlDbType.NVarChar, -1) { Value = input });

        // Add operation-specific parameters
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                var sqlParam = new SqlParameter($"@{param.Key}", param.Value ?? DBNull.Value);
                command.Parameters.Add(sqlParam);
            }
        }

        // Output parameter for results
        var outputParam = new SqlParameter("@result", SqlDbType.NVarChar, -1)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(outputParam);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var result = outputParam.Value as string ?? string.Empty;
        return result;
    }
}

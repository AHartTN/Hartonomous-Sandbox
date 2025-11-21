using Azure.Core;
using Azure.Identity;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces.Conversation;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services.Conversation;

/// <summary>
/// SQL Server implementation of multi-turn conversation service.
/// Provides stateful dialogue management using sp_Converse stored procedure.
/// </summary>
public sealed class SqlConversationService : IConversationService
{
    private readonly string _connectionString;
    private readonly TokenCredential _credential;
    private readonly ILogger<SqlConversationService> _logger;

    public SqlConversationService(
        ILogger<SqlConversationService> logger,
        IOptions<DatabaseOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var databaseOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _connectionString = databaseOptions.HartonomousDb;
        _credential = new DefaultAzureCredential();
    }

    public async Task<ConversationResult> ConverseAsync(
        Guid sessionId,
        string userMessage,
        int tenantId = 0,
        int maxTurns = 10,
        float temperature = 1.0f,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userMessage, nameof(userMessage));

        if (maxTurns < 1 || maxTurns > 100)
            throw new ArgumentOutOfRangeException(nameof(maxTurns), "MaxTurns must be between 1 and 100");

        if (temperature < 0.0f || temperature > 2.0f)
            throw new ArgumentOutOfRangeException(nameof(temperature), "Temperature must be between 0.0 and 2.0");

        _logger.LogInformation(
            "Converse: SessionId {SessionId}, TenantId {TenantId}, MaxTurns {MaxTurns}, Temp {Temperature}",
            sessionId, tenantId, maxTurns, temperature);

        var startTime = DateTime.UtcNow;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_Converse", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120 // 2 minutes for inference
        };

        command.Parameters.AddWithValue("@SessionId", sessionId);
        command.Parameters.AddWithValue("@UserMessage", userMessage);
        command.Parameters.AddWithValue("@TenantId", tenantId);
        command.Parameters.AddWithValue("@MaxTurns", maxTurns);
        command.Parameters.AddWithValue("@Temperature", temperature);

        // Output parameters
        var responseParam = new SqlParameter("@AssistantResponse", SqlDbType.NVarChar, -1)
        {
            Direction = ParameterDirection.Output
        };
        var turnNumberParam = new SqlParameter("@TurnNumber", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        var tokensParam = new SqlParameter("@TokensUsed", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        command.Parameters.Add(responseParam);
        command.Parameters.Add(turnNumberParam);
        command.Parameters.Add(tokensParam);

        await command.ExecuteNonQueryAsync(cancellationToken);
        sw.Stop();

        var response = responseParam.Value?.ToString() ?? string.Empty;
        var turnNumber = turnNumberParam.Value is int turn ? turn : 0;
        var tokensUsed = tokensParam.Value is int tokens ? tokens : (int?)null;

        _logger.LogInformation(
            "Converse completed: SessionId {SessionId}, Turn {Turn}, Response length {Length}, Duration {Duration}ms",
            sessionId, turnNumber, response.Length, sw.ElapsedMilliseconds);

        return new ConversationResult(
            sessionId,
            turnNumber,
            userMessage,
            response,
            startTime,
            tokensUsed,
            (int)sw.ElapsedMilliseconds);
    }

    public async Task<Guid> StartSessionAsync(
        int tenantId = 0,
        string? initialContext = null,
        CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid();

        _logger.LogInformation(
            "StartSession: SessionId {SessionId}, TenantId {TenantId}, HasContext {HasContext}",
            sessionId, tenantId, !string.IsNullOrEmpty(initialContext));

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        // Create session record
        await using var command = new SqlCommand(
            @"INSERT INTO dbo.ConversationSession (SessionId, TenantId, StartedAt, LastActivityAt, InitialContext)
              VALUES (@SessionId, @TenantId, SYSUTCDATETIME(), SYSUTCDATETIME(), @InitialContext)",
            connection);

        command.Parameters.AddWithValue("@SessionId", sessionId);
        command.Parameters.AddWithValue("@TenantId", tenantId);
        command.Parameters.AddWithValue("@InitialContext", (object?)initialContext ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Session created: SessionId {SessionId}", sessionId);

        return sessionId;
    }

    public async Task<IEnumerable<ConversationTurn>> GetHistoryAsync(
        Guid sessionId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1 || limit > 1000)
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 1000");

        _logger.LogInformation("GetHistory: SessionId {SessionId}, Limit {Limit}", sessionId, limit);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand(
            @"SELECT TOP (@Limit) TurnNumber, Role, Message, Timestamp
              FROM dbo.ConversationHistory
              WHERE SessionId = @SessionId
              ORDER BY TurnNumber DESC",
            connection);

        command.Parameters.AddWithValue("@SessionId", sessionId);
        command.Parameters.AddWithValue("@Limit", limit);

        var history = new List<ConversationTurn>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            history.Add(new ConversationTurn(
                reader.GetInt32(0), // TurnNumber
                reader.GetString(1), // Role
                reader.GetString(2), // Message
                reader.GetDateTime(3))); // Timestamp
        }

        _logger.LogInformation("GetHistory retrieved {Count} turns", history.Count);

        return history;
    }

    public async Task ClearHistoryAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ClearHistory: SessionId {SessionId}", sessionId);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand(
            @"DELETE FROM dbo.ConversationHistory WHERE SessionId = @SessionId;
              UPDATE dbo.ConversationSession SET LastActivityAt = SYSUTCDATETIME() WHERE SessionId = @SessionId;",
            connection);

        command.Parameters.AddWithValue("@SessionId", sessionId);

        var rowsDeleted = await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("ClearHistory deleted {Count} turns", rowsDeleted);
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

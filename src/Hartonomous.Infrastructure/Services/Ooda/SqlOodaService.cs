using Azure.Core;
using Azure.Identity;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces.Ooda;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services.Ooda;

/// <summary>
/// SQL Server implementation of OODA loop operations.
/// </summary>
public sealed class SqlOodaService : IOodaService
{
    private readonly string _connectionString;
    private readonly TokenCredential _credential;
    private readonly ILogger<SqlOodaService> _logger;

    public SqlOodaService(
        ILogger<SqlOodaService> logger,
        IOptions<DatabaseOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var databaseOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _connectionString = databaseOptions.HartonomousDb;
        _credential = new DefaultAzureCredential();
    }

    public async Task<AnalysisResult> AnalyzeAsync(
        int tenantId,
        string analysisScope = "full",
        int lookbackHours = 24,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("OODA Analyze: Starting analysis for tenant {TenantId}, scope {Scope}", tenantId, analysisScope);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_Analyze", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

        command.Parameters.AddWithValue("@TenantId", tenantId);
        command.Parameters.AddWithValue("@AnalysisScope", analysisScope);
        command.Parameters.AddWithValue("@LookbackHours", lookbackHours);

        var analysisIdParam = new SqlParameter("@AnalysisId", SqlDbType.UniqueIdentifier) { Direction = ParameterDirection.Output };
        var observationsParam = new SqlParameter("@ObservationsJson", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Output };
        var anomaliesParam = new SqlParameter("@AnomaliesDetected", SqlDbType.Int) { Direction = ParameterDirection.Output };

        command.Parameters.Add(analysisIdParam);
        command.Parameters.Add(observationsParam);
        command.Parameters.Add(anomaliesParam);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return new AnalysisResult(
            (Guid)(analysisIdParam.Value ?? Guid.NewGuid()),
            analysisScope,
            (int)(anomaliesParam.Value ?? 0),
            observationsParam.Value?.ToString() ?? "{}",
            DateTime.UtcNow);
    }

    public async Task<HypothesisResult> HypothesizeAsync(
        Guid analysisId,
        string observationsJson,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("OODA Hypothesize: Generating hypotheses for analysis {AnalysisId}", analysisId);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_Hypothesize", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@AnalysisId", analysisId);
        command.Parameters.AddWithValue("@ObservationsJson", observationsJson);

        var hypothesisIdParam = new SqlParameter("@HypothesisId", SqlDbType.UniqueIdentifier) { Direction = ParameterDirection.Output };
        var hypothesesJsonParam = new SqlParameter("@HypothesesJson", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Output };
        var countParam = new SqlParameter("@HypothesesGenerated", SqlDbType.Int) { Direction = ParameterDirection.Output };

        command.Parameters.Add(hypothesisIdParam);
        command.Parameters.Add(hypothesesJsonParam);
        command.Parameters.Add(countParam);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return new HypothesisResult(
            (Guid)(hypothesisIdParam.Value ?? Guid.NewGuid()),
            analysisId,
            (int)(countParam.Value ?? 0),
            hypothesesJsonParam.Value?.ToString() ?? "[]",
            DateTime.UtcNow);
    }

    public async Task<ActionResult> ActAsync(
        int tenantId,
        int autoApproveThreshold = 3,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("OODA Act: Executing actions for tenant {TenantId}", tenantId);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_Act", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 180
        };

        command.Parameters.AddWithValue("@TenantId", tenantId);
        command.Parameters.AddWithValue("@AutoApproveThreshold", autoApproveThreshold);

        var executedParam = new SqlParameter("@ActionsExecuted", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var skippedParam = new SqlParameter("@ActionsSkipped", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var resultsParam = new SqlParameter("@ResultsJson", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Output };

        command.Parameters.Add(executedParam);
        command.Parameters.Add(skippedParam);
        command.Parameters.Add(resultsParam);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return new ActionResult(
            (int)(executedParam.Value ?? 0),
            (int)(skippedParam.Value ?? 0),
            resultsParam.Value?.ToString() ?? "{}",
            DateTime.UtcNow);
    }

    public async Task<Guid> StartPrimeSearchAsync(
        long rangeStart,
        long rangeEnd,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("OODA StartPrimeSearch: Starting search from {Start} to {End}", rangeStart, rangeEnd);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_StartPrimeSearch", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 30
        };

        command.Parameters.AddWithValue("@RangeStart", rangeStart);
        command.Parameters.AddWithValue("@RangeEnd", rangeEnd);

        var jobIdParam = new SqlParameter("@JobId", SqlDbType.UniqueIdentifier) { Direction = ParameterDirection.Output };
        command.Parameters.Add(jobIdParam);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return (Guid)(jobIdParam.Value ?? Guid.NewGuid());
    }

    private async Task SetupConnectionAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        // Use managed identity for Azure Arc/Azure SQL
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

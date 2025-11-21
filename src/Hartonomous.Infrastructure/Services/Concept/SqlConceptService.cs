using Azure.Core;
using Azure.Identity;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces.Concept;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services.Concept;

/// <summary>
/// SQL Server implementation of concept discovery and binding operations.
/// Executes DBSCAN clustering and Voronoi tessellation via stored procedures.
/// </summary>
public sealed class SqlConceptService : IConceptService
{
    private readonly string _connectionString;
    private readonly TokenCredential _credential;
    private readonly ILogger<SqlConceptService> _logger;

    public SqlConceptService(
        ILogger<SqlConceptService> logger,
        IOptions<DatabaseOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var databaseOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _connectionString = databaseOptions.HartonomousDb;
        _credential = new DefaultAzureCredential();
    }

    public async Task<ConceptDiscoveryResult> DiscoverAndBindAsync(
        int minClusterSize,
        float coherenceThreshold,
        int maxConcepts,
        float similarityThreshold,
        int maxConceptsPerAtom,
        int tenantId,
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minClusterSize);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcepts);

        _logger.LogInformation(
            "DiscoverAndBind: MinCluster {MinCluster}, MaxConcepts {MaxConcepts}, DryRun {DryRun}",
            minClusterSize, maxConcepts, dryRun);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_DiscoverAndBindConcepts", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 300 // 5 minutes - clustering can be expensive
        };

        command.Parameters.AddWithValue("@minClusterSize", minClusterSize);
        command.Parameters.AddWithValue("@coherenceThreshold", coherenceThreshold);
        command.Parameters.AddWithValue("@maxConcepts", maxConcepts);
        command.Parameters.AddWithValue("@similarityThreshold", similarityThreshold);
        command.Parameters.AddWithValue("@maxConceptsPerAtom", maxConceptsPerAtom);
        command.Parameters.AddWithValue("@tenantId", tenantId);
        command.Parameters.AddWithValue("@dryRun", dryRun);

        // Output parameters
        var conceptsDiscoveredParam = new SqlParameter("@conceptsDiscovered", SqlDbType.Int)
        { Direction = ParameterDirection.Output };
        var atomsBoundParam = new SqlParameter("@atomsBound", SqlDbType.Int)
        { Direction = ParameterDirection.Output };
        var avgCoherenceParam = new SqlParameter("@avgCoherence", SqlDbType.Float)
        { Direction = ParameterDirection.Output };

        command.Parameters.Add(conceptsDiscoveredParam);
        command.Parameters.Add(atomsBoundParam);
        command.Parameters.Add(avgCoherenceParam);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var result = new ConceptDiscoveryResult(
            (int)(conceptsDiscoveredParam.Value ?? 0),
            (int)(atomsBoundParam.Value ?? 0),
            0, // BindingsCreated - not tracked
            new List<DiscoveredConcept>()); // Empty list - actual concepts not returned

        _logger.LogInformation(
            "DiscoverAndBind completed: {ConceptsDiscovered} concepts, {AtomsProcessed} atoms processed",
            result.ConceptsDiscovered, result.AtomsProcessed);

        return result;
    }

    public async Task BuildDomainsAsync(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("BuildDomains: TenantId {TenantId}", tenantId);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_BuildConceptDomains", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 600 // 10 minutes - Voronoi tessellation is expensive
        };

        command.Parameters.AddWithValue("@tenantId", tenantId);

        var domainsBuiltParam = new SqlParameter("@domainsBuilt", SqlDbType.Int)
        { Direction = ParameterDirection.Output };
        command.Parameters.Add(domainsBuiltParam);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var domainsBuilt = (int)(domainsBuiltParam.Value ?? 0);

        _logger.LogInformation("BuildDomains completed: {DomainsBuilt} domains built", domainsBuilt);
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

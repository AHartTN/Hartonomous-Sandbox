using Azure.Core;
using Azure.Identity;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces.Discovery;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services.Discovery;

/// <summary>
/// SQL Server implementation of discovery service.
/// Provides unsupervised concept learning and clustering capabilities.
/// </summary>
public sealed class SqlDiscoveryService : IDiscoveryService
{
    private readonly string _connectionString;
    private readonly TokenCredential _credential;
    private readonly ILogger<SqlDiscoveryService> _logger;

    public SqlDiscoveryService(
        ILogger<SqlDiscoveryService> logger,
        IOptions<DatabaseOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var databaseOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _connectionString = databaseOptions.HartonomousDb;
        _credential = new DefaultAzureCredential();
    }

    public async Task<ConceptClusteringResult> ClusterConceptsAsync(
        int tenantId = 0,
        int minClusterSize = 5,
        int maxClusters = 20,
        float densityThreshold = 0.3f,
        CancellationToken cancellationToken = default)
    {
        if (minClusterSize < 2)
            throw new ArgumentOutOfRangeException(nameof(minClusterSize), "MinClusterSize must be at least 2");

        if (maxClusters < 1 || maxClusters > 1000)
            throw new ArgumentOutOfRangeException(nameof(maxClusters), "MaxClusters must be between 1 and 1000");

        if (densityThreshold < 0.0f || densityThreshold > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(densityThreshold), "DensityThreshold must be between 0.0 and 1.0");

        _logger.LogInformation(
            "ClusterConcepts: TenantId {TenantId}, MinSize {MinSize}, MaxClusters {MaxClusters}, Density {Density}",
            tenantId, minClusterSize, maxClusters, densityThreshold);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_ClusterConcepts", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 180 // 3 minutes for clustering
        };

        command.Parameters.AddWithValue("@TenantId", tenantId);
        command.Parameters.AddWithValue("@MinClusterSize", minClusterSize);
        command.Parameters.AddWithValue("@MaxClusters", maxClusters);
        command.Parameters.AddWithValue("@DensityThreshold", densityThreshold);

        // Output parameters
        var clustersCreatedParam = new SqlParameter("@ClustersCreated", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        var atomsProcessedParam = new SqlParameter("@AtomsProcessed", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        var avgSizeParam = new SqlParameter("@AvgClusterSize", SqlDbType.Float)
        {
            Direction = ParameterDirection.Output
        };
        var avgDensityParam = new SqlParameter("@AvgDensity", SqlDbType.Float)
        {
            Direction = ParameterDirection.Output
        };

        command.Parameters.Add(clustersCreatedParam);
        command.Parameters.Add(atomsProcessedParam);
        command.Parameters.Add(avgSizeParam);
        command.Parameters.Add(avgDensityParam);

        await command.ExecuteNonQueryAsync(cancellationToken);
        sw.Stop();

        var clustersCreated = clustersCreatedParam.Value is int clusters ? clusters : 0;
        var atomsProcessed = atomsProcessedParam.Value is int atoms ? atoms : 0;
        var avgSize = avgSizeParam.Value is double size ? (float)size : 0.0f;
        var avgDensity = avgDensityParam.Value is double density ? (float)density : 0.0f;

        _logger.LogInformation(
            "ClusterConcepts completed: Created {Clusters} clusters, Processed {Atoms} atoms, Avg size {AvgSize:F1}, Avg density {AvgDensity:F2}, Duration {Duration}ms",
            clustersCreated, atomsProcessed, avgSize, avgDensity, sw.ElapsedMilliseconds);

        return new ConceptClusteringResult(
            clustersCreated,
            atomsProcessed,
            avgSize,
            avgDensity,
            (int)sw.ElapsedMilliseconds);
    }

    public async Task<IEnumerable<ConceptBinding>> DiscoverAndBindAsync(
        long atomId,
        int maxConcepts = 10,
        float confidenceThreshold = 0.5f,
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(atomId, nameof(atomId));

        if (maxConcepts < 1 || maxConcepts > 100)
            throw new ArgumentOutOfRangeException(nameof(maxConcepts), "MaxConcepts must be between 1 and 100");

        if (confidenceThreshold < 0.0f || confidenceThreshold > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(confidenceThreshold), "ConfidenceThreshold must be between 0.0 and 1.0");

        _logger.LogInformation(
            "DiscoverAndBind: AtomId {AtomId}, MaxConcepts {MaxConcepts}, Threshold {Threshold}",
            atomId, maxConcepts, confidenceThreshold);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_DiscoverAndBindConcepts", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@AtomId", atomId);
        command.Parameters.AddWithValue("@MaxConcepts", maxConcepts);
        command.Parameters.AddWithValue("@ConfidenceThreshold", confidenceThreshold);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        var bindings = new List<ConceptBinding>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            bindings.Add(new ConceptBinding(
                reader.GetInt32(reader.GetOrdinal("ConceptId")),
                reader.GetString(reader.GetOrdinal("ConceptName")),
                (float)reader.GetDouble(reader.GetOrdinal("Confidence")),
                (float)reader.GetDouble(reader.GetOrdinal("Relevance"))));
        }

        _logger.LogInformation("DiscoverAndBind found {Count} concept bindings", bindings.Count);

        return bindings;
    }

    public async Task<ConceptDomainResult> BuildDomainsAsync(
        int tenantId = 0,
        int maxDepth = 5,
        CancellationToken cancellationToken = default)
    {
        if (maxDepth < 1 || maxDepth > 10)
            throw new ArgumentOutOfRangeException(nameof(maxDepth), "MaxDepth must be between 1 and 10");

        _logger.LogInformation("BuildDomains: TenantId {TenantId}, MaxDepth {MaxDepth}", tenantId, maxDepth);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_BuildConceptDomains", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

        command.Parameters.AddWithValue("@TenantId", tenantId);
        command.Parameters.AddWithValue("@MaxDepth", maxDepth);

        // Output parameters
        var domainsCreatedParam = new SqlParameter("@DomainsCreated", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        var maxDepthParam = new SqlParameter("@ActualMaxDepth", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        var totalConceptsParam = new SqlParameter("@TotalConcepts", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        command.Parameters.Add(domainsCreatedParam);
        command.Parameters.Add(maxDepthParam);
        command.Parameters.Add(totalConceptsParam);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var domainsCreated = domainsCreatedParam.Value is int domains ? domains : 0;
        var actualMaxDepth = maxDepthParam.Value is int depth ? depth : 0;
        var totalConcepts = totalConceptsParam.Value is int concepts ? concepts : 0;

        _logger.LogInformation(
            "BuildDomains completed: Created {Domains} domains, Depth {Depth}, Total concepts {Total}",
            domainsCreated, actualMaxDepth, totalConcepts);

        return new ConceptDomainResult(domainsCreated, actualMaxDepth, totalConcepts);
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

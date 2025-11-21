using Azure.Core;
using Azure.Identity;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces.Search;
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

namespace Hartonomous.Infrastructure.Services.Search;

/// <summary>
/// SQL Server implementation of search operations.
/// </summary>
public sealed class SqlSearchService : ISearchService
{
    private readonly string _connectionString;
    private readonly TokenCredential _credential;
    private readonly ILogger<SqlSearchService> _logger;

    public SqlSearchService(
        ILogger<SqlSearchService> logger,
        IOptions<DatabaseOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var databaseOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _connectionString = databaseOptions.HartonomousDb;
        _credential = new DefaultAzureCredential();
    }

    public async Task<IEnumerable<SearchResult>> SemanticSearchAsync(
        string queryText,
        int topK = 10,
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SemanticSearch: Query '{Query}', TopK {TopK}", queryText?.Substring(0, Math.Min(50, queryText?.Length ?? 0)), topK);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_SemanticSearch", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@QueryText", queryText);
        command.Parameters.AddWithValue("@TopK", topK);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        return await ReadSearchResultsAsync(command, cancellationToken);
    }

    public async Task<IEnumerable<SearchResult>> HybridSearchAsync(
        string textQuery,
        byte[] queryVector,
        int topK = 10,
        float textWeight = 0.4f,
        float vectorWeight = 0.6f,
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HybridSearch: TextWeight {TextWeight}, VectorWeight {VectorWeight}", textWeight, vectorWeight);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_HybridSearch", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@TextQuery", textQuery);
        command.Parameters.AddWithValue("@QueryVector", queryVector);
        command.Parameters.AddWithValue("@TopK", topK);
        command.Parameters.AddWithValue("@TextWeight", textWeight);
        command.Parameters.AddWithValue("@VectorWeight", vectorWeight);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        return await ReadSearchResultsAsync(command, cancellationToken);
    }

    public async Task<IEnumerable<SearchResult>> FusionSearchAsync(
        byte[] queryVector,
        string? keywords = null,
        Geometry? spatialRegion = null,
        int topK = 10,
        float vectorWeight = 0.5f,
        float keywordWeight = 0.3f,
        float spatialWeight = 0.2f,
        int? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("FusionSearch: Vector {VW}, Keyword {KW}, Spatial {SW}", vectorWeight, keywordWeight, spatialWeight);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_FusionSearch", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@QueryVector", queryVector);
        command.Parameters.AddWithValue("@Keywords", (object?)keywords ?? DBNull.Value);
        command.Parameters.AddWithValue("@SpatialRegion", spatialRegion != null ? new WKTWriter().Write(spatialRegion) : DBNull.Value);
        command.Parameters.AddWithValue("@TopK", topK);
        command.Parameters.AddWithValue("@VectorWeight", vectorWeight);
        command.Parameters.AddWithValue("@KeywordWeight", keywordWeight);
        command.Parameters.AddWithValue("@SpatialWeight", spatialWeight);
        command.Parameters.AddWithValue("@TenantId", (object?)tenantId ?? DBNull.Value);

        return await ReadSearchResultsAsync(command, cancellationToken);
    }

    public async Task<IEnumerable<SearchResult>> ExactVectorSearchAsync(
        byte[] queryVector,
        int topK = 10,
        int tenantId = 0,
        string distanceMetric = "cosine",
        string? embeddingType = null,
        int? modelId = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_ExactVectorSearch", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

        command.Parameters.AddWithValue("@query_vector", queryVector);
        command.Parameters.AddWithValue("@top_k", topK);
        command.Parameters.AddWithValue("@TenantId", tenantId);
        command.Parameters.AddWithValue("@distance_metric", distanceMetric);
        command.Parameters.AddWithValue("@embedding_type", (object?)embeddingType ?? DBNull.Value);
        command.Parameters.AddWithValue("@ModelId", (object?)modelId ?? DBNull.Value);

        return await ReadSearchResultsAsync(command, cancellationToken);
    }

    public async Task<IEnumerable<SearchResult>> FilteredSearchAsync(
        byte[] queryVector,
        string filtersJson,
        int topK = 10,
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_SemanticFilteredSearch", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@QueryVector", queryVector);
        command.Parameters.AddWithValue("@Filters", filtersJson);
        command.Parameters.AddWithValue("@TopK", topK);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        return await ReadSearchResultsAsync(command, cancellationToken);
    }

    public async Task<IEnumerable<SearchResult>> TemporalSearchAsync(
        byte[] queryVector,
        DateTime startDate,
        DateTime endDate,
        int topK = 10,
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_TemporalVectorSearch", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@QueryVector", queryVector);
        command.Parameters.AddWithValue("@StartDate", startDate);
        command.Parameters.AddWithValue("@EndDate", endDate);
        command.Parameters.AddWithValue("@TopK", topK);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        return await ReadSearchResultsAsync(command, cancellationToken);
    }

    public async Task<IEnumerable<SearchResult>> CrossModalSearchAsync(
        string? textQuery = null,
        float? spatialX = null,
        float? spatialY = null,
        float? spatialZ = null,
        string? modalityFilter = null,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_CrossModalQuery", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@text_query", (object?)textQuery ?? DBNull.Value);
        command.Parameters.AddWithValue("@spatial_query_x", (object?)spatialX ?? DBNull.Value);
        command.Parameters.AddWithValue("@spatial_query_y", (object?)spatialY ?? DBNull.Value);
        command.Parameters.AddWithValue("@spatial_query_z", (object?)spatialZ ?? DBNull.Value);
        command.Parameters.AddWithValue("@modality_filter", (object?)modalityFilter ?? DBNull.Value);
        command.Parameters.AddWithValue("@top_k", topK);

        return await ReadSearchResultsAsync(command, cancellationToken);
    }

    private async Task<List<SearchResult>> ReadSearchResultsAsync(SqlCommand command, CancellationToken cancellationToken)
    {
        var results = new List<SearchResult>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new SearchResult(
                reader.GetInt64(reader.GetOrdinal("AtomId")),
                reader.IsDBNull(reader.GetOrdinal("Score")) ? 0 : (float)reader.GetDouble(reader.GetOrdinal("Score")),
                reader.IsDBNull(reader.GetOrdinal("Modality")) ? null : reader.GetString(reader.GetOrdinal("Modality")),
                reader.IsDBNull(reader.GetOrdinal("ContentPreview")) ? null : reader.GetString(reader.GetOrdinal("ContentPreview")),
                reader.IsDBNull(reader.GetOrdinal("CreatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            ));
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

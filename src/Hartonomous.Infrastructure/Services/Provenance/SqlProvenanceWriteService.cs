using Azure.Core;
using Azure.Identity;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces.Provenance;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services.Provenance;

/// <summary>
/// SQL Server implementation of provenance write operations.
/// </summary>
public sealed class SqlProvenanceWriteService : IProvenanceWriteService
{
    private readonly string _connectionString;
    private readonly TokenCredential _credential;
    private readonly ILogger<SqlProvenanceWriteService> _logger;

    public SqlProvenanceWriteService(
        ILogger<SqlProvenanceWriteService> logger,
        IOptions<DatabaseOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var databaseOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _connectionString = databaseOptions.HartonomousDb;
        _credential = new DefaultAzureCredential();
    }

    public async Task LinkProvenanceAsync(
        string parentAtomIds,
        long childAtomId,
        string dependencyType = "DerivedFrom",
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_LinkProvenance", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 30
        };

        command.Parameters.AddWithValue("@ParentAtomIds", parentAtomIds);
        command.Parameters.AddWithValue("@ChildAtomId", childAtomId);
        command.Parameters.AddWithValue("@DependencyType", dependencyType);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Linked provenance for atom {AtomId}", childAtomId);
    }

    public async Task<LineageResult> QueryLineageAsync(
        long atomId,
        int tenantId = 0,
        string direction = "both",
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_QueryLineage", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@AtomId", atomId);
        command.Parameters.AddWithValue("@TenantId", tenantId);
        command.Parameters.AddWithValue("@Direction", direction);

        var upstream = new List<LineageNode>();
        var downstream = new List<LineageNode>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        // First result set: upstream
        while (await reader.ReadAsync(cancellationToken))
        {
            upstream.Add(new LineageNode(
                reader.GetInt64(reader.GetOrdinal("AtomId")),
                reader.GetInt32(reader.GetOrdinal("Depth")),
                reader.GetString(reader.GetOrdinal("RelationType"))));
        }

        // Second result set: downstream
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                downstream.Add(new LineageNode(
                    reader.GetInt64(reader.GetOrdinal("AtomId")),
                    reader.GetInt32(reader.GetOrdinal("Depth")),
                    reader.GetString(reader.GetOrdinal("RelationType"))));
            }
        }

        return new LineageResult(atomId, upstream, downstream);
    }

    public async Task<string> ExportProvenanceAsync(
        long atomId,
        string format = "JSON",
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_ExportProvenance", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@AtomId", atomId);
        command.Parameters.AddWithValue("@Format", format);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        var outputParam = new SqlParameter("@ExportData", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Output };
        command.Parameters.Add(outputParam);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return outputParam.Value?.ToString() ?? "";
    }

    public async Task<ValidationResult> ValidateProvenanceAsync(
        Guid operationId,
        string? expectedScope = null,
        string? expectedModel = null,
        int minSegments = 1,
        int maxAgeHours = 24,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_ValidateOperationProvenance", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 30
        };

        command.Parameters.AddWithValue("@OperationId", operationId);
        command.Parameters.AddWithValue("@ExpectedScope", (object?)expectedScope ?? DBNull.Value);
        command.Parameters.AddWithValue("@ExpectedModel", (object?)expectedModel ?? DBNull.Value);
        command.Parameters.AddWithValue("@MinSegments", minSegments);
        command.Parameters.AddWithValue("@MaxAgeHours", maxAgeHours);

        var isValidParam = new SqlParameter("@IsValid", SqlDbType.Bit) { Direction = ParameterDirection.Output };
        var segmentsParam = new SqlParameter("@SegmentsValidated", SqlDbType.Int) { Direction = ParameterDirection.Output };
        command.Parameters.Add(isValidParam);
        command.Parameters.Add(segmentsParam);

        var errors = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            errors.Add(reader.GetString(0));
        }

        return new ValidationResult(
            (bool)(isValidParam.Value ?? false),
            (int)(segmentsParam.Value ?? 0),
            errors);
    }

    public async Task<AuditResult> AuditProvenanceAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? scope = null,
        float minValidationScore = 0.8f,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_AuditProvenanceChain", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

        command.Parameters.AddWithValue("@StartDate", (object?)startDate ?? DBNull.Value);
        command.Parameters.AddWithValue("@EndDate", (object?)endDate ?? DBNull.Value);
        command.Parameters.AddWithValue("@Scope", (object?)scope ?? DBNull.Value);
        command.Parameters.AddWithValue("@MinValidationScore", minValidationScore);

        var anomalies = new List<AuditAnomaly>();
        int chainsAudited = 0;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            anomalies.Add(new AuditAnomaly(
                reader.GetGuid(reader.GetOrdinal("OperationId")),
                reader.GetString(reader.GetOrdinal("AnomalyType")),
                reader.GetString(reader.GetOrdinal("Description")),
                (float)reader.GetDouble(reader.GetOrdinal("Severity"))));
        }

        if (await reader.NextResultAsync(cancellationToken) && await reader.ReadAsync(cancellationToken))
        {
            chainsAudited = reader.GetInt32(0);
        }

        return new AuditResult(chainsAudited, anomalies.Count, anomalies);
    }

    public async Task<IEnumerable<ImpactedAtom>> FindImpactedAtomsAsync(
        long atomId,
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_FindImpactedAtoms", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@AtomId", atomId);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        var results = new List<ImpactedAtom>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ImpactedAtom(
                reader.GetInt64(reader.GetOrdinal("AtomId")),
                reader.GetInt32(reader.GetOrdinal("Depth")),
                reader.GetString(reader.GetOrdinal("ImpactPath"))));
        }

        return results;
    }

    public async Task<IEnumerable<RelatedDocument>> FindRelatedDocumentsAsync(
        long atomId,
        int topK = 10,
        int tenantId = 0,
        bool includeSemanticText = true,
        bool includeVectorSimilarity = true,
        bool includeGraphNeighbors = true,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_FindRelatedDocuments", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@AtomId", atomId);
        command.Parameters.AddWithValue("@TopK", topK);
        command.Parameters.AddWithValue("@TenantId", tenantId);
        command.Parameters.AddWithValue("@IncludeSemanticText", includeSemanticText);
        command.Parameters.AddWithValue("@IncludeVectorSimilarity", includeVectorSimilarity);
        command.Parameters.AddWithValue("@IncludeGraphNeighbors", includeGraphNeighbors);

        var results = new List<RelatedDocument>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new RelatedDocument(
                reader.GetInt64(reader.GetOrdinal("AtomId")),
                reader.GetString(reader.GetOrdinal("RelationType")),
                (float)reader.GetDouble(reader.GetOrdinal("RelevanceScore"))));
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

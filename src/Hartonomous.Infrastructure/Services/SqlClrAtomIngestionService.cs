using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// SQL CLR-based implementation of atom ingestion that calls intelligent stored procedures
/// instead of using EF Core repositories. This preserves autonomous AI capabilities.
/// </summary>
public class SqlClrAtomIngestionService : IAtomIngestionService
{
    private readonly string _connectionString;
    private readonly ILogger<SqlClrAtomIngestionService> _logger;

    public SqlClrAtomIngestionService(
        IConfiguration configuration,
        ILogger<SqlClrAtomIngestionService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new ArgumentNullException("DefaultConnection connection string not found");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Ingests an atom using the intelligent sp_AtomIngestion stored procedure
    /// which performs autonomous deduplication and embedding storage.
    /// </summary>
    public async Task<AtomIngestionResult> IngestAsync(
        AtomIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.HashInput))
        {
            throw new ArgumentException("HashInput must be provided", nameof(request));
        }

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = new SqlCommand("dbo.sp_AtomIngestion", connection);
        command.CommandType = CommandType.StoredProcedure;

        // Input parameters
        command.Parameters.AddWithValue("@HashInput", request.HashInput);
        command.Parameters.AddWithValue("@Modality", request.Modality);
        command.Parameters.AddWithValue("@Subtype", request.Subtype ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SourceUri", request.SourceUri ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SourceType", request.SourceType ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CanonicalText", request.CanonicalText ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Metadata", request.Metadata ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PayloadLocator", request.PayloadLocator ?? (object)DBNull.Value);

        // Handle embedding vector
        if (request.Embedding != null)
        {
            // Convert float[] to SQL VECTOR format (JSON array)
            var embeddingJson = "[" + string.Join(",", request.Embedding.Select(v => v.ToString("F6"))) + "]";
            command.Parameters.AddWithValue("@Embedding", embeddingJson);
        }
        else
        {
            command.Parameters.AddWithValue("@Embedding", DBNull.Value);
        }

        command.Parameters.AddWithValue("@EmbeddingType", request.EmbeddingType);
        command.Parameters.AddWithValue("@ModelId", request.ModelId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PolicyName", request.PolicyName);
        command.Parameters.AddWithValue("@TenantId", 0); // Default tenant

        // Output parameters
        var atomIdParam = new SqlParameter("@AtomId", SqlDbType.BigInt) { Direction = ParameterDirection.Output };
        var atomEmbeddingIdParam = new SqlParameter("@AtomEmbeddingId", SqlDbType.BigInt) { Direction = ParameterDirection.Output };
        var wasDuplicateParam = new SqlParameter("@WasDuplicate", SqlDbType.Bit) { Direction = ParameterDirection.Output };
        var duplicateReasonParam = new SqlParameter("@DuplicateReason", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output };
        var semanticSimilarityParam = new SqlParameter("@SemanticSimilarity", SqlDbType.Float) { Direction = ParameterDirection.Output };

        command.Parameters.Add(atomIdParam);
        command.Parameters.Add(atomEmbeddingIdParam);
        command.Parameters.Add(wasDuplicateParam);
        command.Parameters.Add(duplicateReasonParam);
        command.Parameters.Add(semanticSimilarityParam);

        await command.ExecuteNonQueryAsync(cancellationToken);

        // Extract output values
        var atomId = (long)atomIdParam.Value;
        var atomEmbeddingId = atomEmbeddingIdParam.Value != DBNull.Value ? (long?)atomEmbeddingIdParam.Value : null;
        var wasDuplicate = (bool)wasDuplicateParam.Value;
        var duplicateReason = duplicateReasonParam.Value != DBNull.Value ? (string)duplicateReasonParam.Value : null;
        var semanticSimilarity = semanticSimilarityParam.Value != DBNull.Value ? (double?)semanticSimilarityParam.Value : null;

        // Fetch the complete atom entity
        var atom = await GetAtomByIdAsync(connection, atomId, cancellationToken);
        AtomEmbedding? embedding = null;

        if (atomEmbeddingId.HasValue)
        {
            embedding = await GetAtomEmbeddingByIdAsync(connection, atomEmbeddingId.Value, cancellationToken);
        }

        if (wasDuplicate)
        {
            _logger.LogInformation(
                "Atom ingestion: Reused existing atom {AtomId} (Reason: {Reason})",
                atomId,
                duplicateReason ?? "unknown");
        }
        else
        {
            _logger.LogInformation(
                "Atom ingestion: Created new atom {AtomId} with embedding {EmbeddingId}",
                atomId,
                atomEmbeddingId);
        }

        return new AtomIngestionResult
        {
            Atom = atom,
            Embedding = embedding,
            WasDuplicate = wasDuplicate,
            DuplicateReason = duplicateReason,
            SemanticSimilarity = semanticSimilarity
        };
    }

    private async Task<Atom> GetAtomByIdAsync(SqlConnection connection, long atomId, CancellationToken cancellationToken)
    {
        using var command = new SqlCommand(@"
            SELECT AtomId, ContentHash, Modality, Subtype, SourceUri, SourceType,
                   CanonicalText, Metadata, PayloadLocator, ReferenceCount,
                   CreatedAt, UpdatedAt, IsActive
            FROM dbo.Atoms
            WHERE AtomId = @AtomId", connection);

        command.Parameters.AddWithValue("@AtomId", atomId);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new Atom
            {
                AtomId = reader.GetInt64(0),
                ContentHash = (byte[])reader[1],
                Modality = reader.GetString(2),
                Subtype = reader.IsDBNull(3) ? null : reader.GetString(3),
                SourceUri = reader.IsDBNull(4) ? null : reader.GetString(4),
                SourceType = reader.IsDBNull(5) ? null : reader.GetString(5),
                CanonicalText = reader.IsDBNull(6) ? null : reader.GetString(6),
                Metadata = reader.IsDBNull(7) ? null : reader.GetString(7),
                PayloadLocator = reader.IsDBNull(8) ? null : reader.GetString(8),
                ReferenceCount = reader.GetInt64(9),
                CreatedAt = reader.GetDateTime(10),
                UpdatedAt = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                IsActive = reader.GetBoolean(12),
                Embeddings = new List<AtomEmbedding>() // Will be populated if needed
            };
        }

        throw new InvalidOperationException($"Atom with ID {atomId} not found");
    }

    private async Task<AtomEmbedding> GetAtomEmbeddingByIdAsync(SqlConnection connection, long atomEmbeddingId, CancellationToken cancellationToken)
    {
        using var command = new SqlCommand(@"
            SELECT AtomEmbeddingId, AtomId, EmbeddingVector, EmbeddingType, ModelId,
                   Dimension, SpatialGeometry, SpatialCoarse, SpatialBucketX, SpatialBucketY, SpatialBucketZ,
                   Metadata, CreatedAt
            FROM dbo.AtomEmbeddings
            WHERE AtomEmbeddingId = @AtomEmbeddingId", connection);

        command.Parameters.AddWithValue("@AtomEmbeddingId", atomEmbeddingId);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new AtomEmbedding
            {
                AtomEmbeddingId = reader.GetInt64(0),
                AtomId = reader.GetInt64(1),
                // Note: EmbeddingVector is stored as VECTOR type, would need conversion
                EmbeddingType = reader.GetString(3),
                ModelId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                Dimension = reader.GetInt32(5),
                // SpatialGeometry and other spatial fields would need conversion
                Metadata = reader.IsDBNull(11) ? null : reader.GetString(11),
                CreatedAt = reader.GetDateTime(12)
            };
        }

        throw new InvalidOperationException($"Atom embedding with ID {atomEmbeddingId} not found");
    }
}
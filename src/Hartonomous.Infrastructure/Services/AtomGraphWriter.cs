using System.Buffers;
using System.Data;
using System.Data.SqlTypes;
using System.Text.Json;
using System.Text;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SqlServer.Types;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Maintains the SQL graph node and edge tables alongside the relational atom store.
/// </summary>
public sealed class AtomGraphWriter : IAtomGraphWriter
{
    private readonly ISqlCommandExecutor _executor;
    private readonly IOptionsMonitor<AtomGraphOptions> _options;
    private readonly ILogger<AtomGraphWriter> _logger;
    private readonly SqlServerBytesWriter _geometryWriter = new()
    {
        IsGeography = false,
        HandleOrdinates = Ordinates.XYZ
    };

    public AtomGraphWriter(
        ISqlCommandExecutor executor,
        IOptionsMonitor<AtomGraphOptions> options,
        ILogger<AtomGraphWriter> logger)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task UpsertAtomNodeAsync(Atom atom, AtomEmbedding? primaryEmbedding, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(atom);

        if (!_options.CurrentValue.EnableSqlGraphWrites)
        {
            return Task.CompletedTask;
        }

        var semantics = SerializeSemantics(atom, primaryEmbedding);
        var sqlSpatialKey = ToSqlGeometry(atom.SpatialKey);

        return _executor.ExecuteAsync(async (command, ct) =>
        {
            command.CommandText = """
MERGE graph.AtomGraphNodes AS target
USING (SELECT @AtomId AS AtomId) AS source
ON target.AtomId = source.AtomId
WHEN MATCHED THEN
    UPDATE SET
        Modality = @Modality,
        Subtype = @Subtype,
        SourceType = @SourceType,
        SourceUri = @SourceUri,
        PayloadLocator = @PayloadLocator,
        CanonicalText = @CanonicalText,
        Metadata = @Metadata,
        Semantics = @Semantics,
        SpatialKey = @SpatialKey,
        UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (AtomId, Modality, Subtype, SourceType, SourceUri, PayloadLocator, CanonicalText, Metadata, Semantics, SpatialKey, CreatedAt, UpdatedAt)
    VALUES (@AtomId, @Modality, @Subtype, @SourceType, @SourceUri, @PayloadLocator, @CanonicalText, @Metadata, @Semantics, @SpatialKey, SYSUTCDATETIME(), SYSUTCDATETIME());
""";

            command.Parameters.Clear();
            command.Parameters.Add(CreateParameter(command, "@AtomId", atom.AtomId));
            command.Parameters.Add(CreateParameter(command, "@Modality", atom.Modality));
            command.Parameters.Add(CreateParameter(command, "@Subtype", atom.Subtype));
            command.Parameters.Add(CreateParameter(command, "@SourceType", atom.SourceType));
            command.Parameters.Add(CreateParameter(command, "@SourceUri", atom.SourceUri));
            command.Parameters.Add(CreateParameter(command, "@PayloadLocator", atom.PayloadLocator));
            command.Parameters.Add(CreateParameter(command, "@CanonicalText", atom.CanonicalText));
            command.Parameters.Add(CreateParameter(command, "@Metadata", atom.Metadata, SqlDbType.Json));
            command.Parameters.Add(CreateParameter(command, "@Semantics", semantics, SqlDbType.Json));
            command.Parameters.Add(CreateParameter(command, "@SpatialKey", sqlSpatialKey, SqlDbType.Udt, "geometry"));

            await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }, cancellationToken);
    }

    public async Task UpsertRelationAsync(AtomRelation relation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(relation);

        if (!_options.CurrentValue.EnableSqlGraphWrites)
        {
            return;
        }

        var sqlSpatialExpression = ToSqlGeometry(relation.SpatialExpression);

        async Task ExecuteUpsertAsync(CancellationToken ct)
        {
            await _executor.ExecuteAsync(async (command, innerCt) =>
            {
                command.CommandText = """
DECLARE @FromNodeId BIGINT;
DECLARE @ToNodeId BIGINT;

SELECT @FromNodeId = nodes.$node_id
FROM graph.AtomGraphNodes AS nodes
WHERE nodes.AtomId = @SourceAtomId;

SELECT @ToNodeId = nodes.$node_id
FROM graph.AtomGraphNodes AS nodes
WHERE nodes.AtomId = @TargetAtomId;

IF @FromNodeId IS NULL OR @ToNodeId IS NULL
BEGIN
    THROW 50052, 'Source or target node missing for atom relation.', 1;
END;

MERGE graph.AtomGraphEdges AS target
USING (
using Hartonomous.Data.Entities;
    SELECT
        @AtomRelationId AS AtomRelationId,
        @FromNodeId AS FromNodeId,
        @ToNodeId AS ToNodeId,
        @RelationType AS RelationType,
        @Weight AS Weight,
        @Metadata AS Metadata,
        @SpatialExpression AS SpatialExpression,
        @CreatedAt AS CreatedAt
) AS source
ON target.AtomRelationId = source.AtomRelationId
WHEN MATCHED THEN
    UPDATE SET
        RelationType = source.RelationType,
        Weight = source.Weight,
        Metadata = source.Metadata,
        SpatialExpression = source.SpatialExpression,
        CreatedAt = source.CreatedAt,
        UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT ($from_id, $to_id, AtomRelationId, RelationType, Weight, Metadata, SpatialExpression, CreatedAt, UpdatedAt)
    VALUES (source.FromNodeId, source.ToNodeId, source.AtomRelationId, source.RelationType, source.Weight, source.Metadata, source.SpatialExpression, source.CreatedAt, SYSUTCDATETIME());
""";

                command.Parameters.Clear();
                command.Parameters.Add(CreateParameter(command, "@AtomRelationId", relation.AtomRelationId));
                command.Parameters.Add(CreateParameter(command, "@SourceAtomId", relation.SourceAtomId));
                command.Parameters.Add(CreateParameter(command, "@TargetAtomId", relation.TargetAtomId));
                command.Parameters.Add(CreateParameter(command, "@RelationType", relation.RelationType));
                command.Parameters.Add(CreateParameter(command, "@Weight", relation.Weight));
                command.Parameters.Add(CreateParameter(command, "@Metadata", relation.Metadata, SqlDbType.Json));
                command.Parameters.Add(CreateParameter(command, "@SpatialExpression", sqlSpatialExpression, SqlDbType.Udt, "geometry"));
                command.Parameters.Add(CreateParameter(command, "@CreatedAt", relation.CreatedAt));

                await command.ExecuteNonQueryAsync(innerCt).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);
        }

        var retried = false;
        try
        {
            await ExecuteUpsertAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SqlException ex) when (ex.Number == 50052 && _options.CurrentValue.EnableSynchronizationJob && !retried)
        {
            retried = true;
            _logger.LogInformation(ex, "Graph node missing for relation {RelationId}; attempting synchronization before retry.", relation.AtomRelationId);
            await SynchronizeAsync(cancellationToken).ConfigureAwait(false);
            await ExecuteUpsertAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public Task SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.CurrentValue.EnableSynchronizationJob)
        {
            return Task.CompletedTask;
        }

        return _executor.ExecuteAsync(async (command, ct) =>
        {
            command.CommandText = "EXEC graph.usp_SyncAtomGraphFromRelations @BatchSize";
            command.Parameters.Clear();
            command.Parameters.Add(CreateParameter(command, "@BatchSize", _options.CurrentValue.SyncBatchSize));
            await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }, cancellationToken);
    }

    private static string? SerializeSemantics(Atom atom, AtomEmbedding? primaryEmbedding)
    {
        var hasEmbedding = primaryEmbedding is not null;
        var hasCanonicalText = !string.IsNullOrWhiteSpace(atom.CanonicalText);
        var hasMetadata = !string.IsNullOrWhiteSpace(atom.Metadata);
        var hasPayloadLocator = !string.IsNullOrWhiteSpace(atom.PayloadLocator);

        if (!hasEmbedding && !hasCanonicalText && !hasMetadata && !hasPayloadLocator)
        {
            return null;
        }

        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            WriteString(writer, "modality", atom.Modality);
            WriteString(writer, "subtype", atom.Subtype);
            WriteString(writer, "sourceType", atom.SourceType);
            WriteString(writer, "sourceUri", atom.SourceUri);
            WriteString(writer, "payloadLocator", atom.PayloadLocator);
            WriteString(writer, "canonicalText", atom.CanonicalText);
            WriteString(writer, "metadata", atom.Metadata);

            if (primaryEmbedding is not null)
            {
                writer.WritePropertyName("embedding");
                writer.WriteStartObject();
                WriteString(writer, "embeddingType", primaryEmbedding.EmbeddingType);
                writer.WriteNumber("dimension", primaryEmbedding.Dimension);
                if (primaryEmbedding.ModelId.HasValue)
                {
                    writer.WriteNumber("modelId", primaryEmbedding.ModelId.Value);
                }

                // UsesMaxDimensionPadding property no longer exists on AtomEmbedding
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static SqlParameter CreateParameter(SqlCommand command, string name, object? value, SqlDbType? sqlDbType = null, string? udtTypeName = null)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        if (sqlDbType.HasValue)
        {
            parameter.SqlDbType = sqlDbType.Value;
        }

        if (!string.IsNullOrWhiteSpace(udtTypeName))
        {
            parameter.UdtTypeName = udtTypeName;
        }

        parameter.Value = value ?? DBNull.Value;
        return parameter;
    }

    private static void WriteString(Utf8JsonWriter writer, string propertyName, string? value)
    {
        if (value is not null)
        {
            writer.WriteString(propertyName, value);
        }
    }

    private SqlGeometry? ToSqlGeometry(Geometry? source)
    {
        if (source is null)
        {
            return null;
        }

        var srid = source.SRID >= 0 ? source.SRID : 0;
        var bytes = _geometryWriter.Write(source);
        return SqlGeometry.STGeomFromWKB(new SqlBytes(bytes), srid);
    }
}

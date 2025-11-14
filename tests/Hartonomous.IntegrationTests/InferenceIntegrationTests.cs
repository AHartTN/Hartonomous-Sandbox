using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Hartonomous.Core.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hartonomous.IntegrationTests;

/// <summary>
/// Integration tests that exercise the production services against the live SQL Server database.
/// </summary>
public sealed class InferenceIntegrationTests : IntegrationTestBase
{
    public InferenceIntegrationTests(SqlServerTestFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task SemanticSearch_ReturnsOriginalEmbedding()
    {
        var activeConnectionString = Fixture.DbContext!.Database.GetConnectionString() ?? Fixture.ConnectionString;
        Assert.False(string.IsNullOrWhiteSpace(activeConnectionString), "DbContext connection string was not initialised.");

        var sample = await Fixture.DbContext!.AtomEmbeddings
            .Include(e => e.AtomEmbeddingComponents)
            .Include(e => e.Atom)
            .Where(e => !e.EmbeddingVector.IsNull)
            .OrderBy(e => e.AtomEmbeddingId)
            .FirstOrDefaultAsync();

        Assert.NotNull(sample);

        Assert.NotNull(sample.EmbeddingVector);
        var vector = VectorUtility.Materialize(sample!.EmbeddingVector, sample.Dimension);
        Assert.NotEmpty(vector);

        var results = await Fixture.InferenceService!
            .SemanticSearchAsync(vector, topK: 5);

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Embedding.AtomEmbeddingId == sample.AtomEmbeddingId);
    }

    [Fact]
    public async Task HybridSearch_ReturnsSpatialCandidates()
    {
        var sample = await Fixture.DbContext!.AtomEmbeddings
            .Include(e => e.AtomEmbeddingComponents)
            .Where(e => !e.EmbeddingVector.IsNull)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync();

        Assert.NotNull(sample);

        Assert.NotNull(sample.EmbeddingVector);
        var queryVector = VectorUtility.Materialize(sample!.EmbeddingVector, sample.Dimension);
        Assert.NotEmpty(queryVector);

        var results = await Fixture.InferenceService!
            .HybridSearchAsync(queryVector, topK: 5, candidateCount: 32);

        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task ComputeSpatialProjection_ProducesGeometry()
    {
        var sample = await Fixture.DbContext!.AtomEmbeddings
            .Include(e => e.AtomEmbeddingComponents)
            .Where(e => !e.EmbeddingVector.IsNull)
            .OrderBy(e => e.AtomEmbeddingId)
            .FirstOrDefaultAsync();

        Assert.NotNull(sample);

        Assert.NotNull(sample.EmbeddingVector);
        var dense = VectorUtility.Materialize(sample!.EmbeddingVector, sample.Dimension);
        var padded = VectorUtility.PadToSqlLength(dense, out _);
        var spatialPoint = await Fixture.AtomEmbeddings!
            .ComputeSpatialProjectionAsync(new SqlVector<float>(padded), dense.Length);

        Assert.NotNull(spatialPoint);
        Assert.False(double.IsNaN(spatialPoint.X));
        Assert.False(double.IsNaN(spatialPoint.Y));
    }

    [Fact]
    public async Task MultiResolutionSearch_UsesSpatialCoordinates()
    {
        var sample = await Fixture.DbContext!.AtomEmbeddings
            .Where(e => e.SpatialGeometry != null)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync();

        Assert.NotNull(sample);
        Assert.NotNull(sample!.SpatialGeometry);

        var spatial = (NetTopologySuite.Geometries.Point)sample.SpatialGeometry!;
        var results = await Fixture.SpatialService!
            .MultiResolutionSearchAsync(spatial.X, spatial.Y, spatial.Z, coarseCandidates: 32, fineCandidates: 16, topK: 5);

        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task StudentModelComparison_ReturnsMetrics()
    {
        var parentModel = await Fixture.DbContext!.Models
            .OrderBy(m => m.ModelId)
            .FirstOrDefaultAsync();

        Assert.NotNull(parentModel);

        var student = await Fixture.StudentModelService!
            .ExtractByImportanceAsync(parentModel!.ModelId, targetSizeRatio: 0.25);

        var comparison = await Fixture.StudentModelService!
            .CompareModelsAsync(parentModel.ModelId, student.ModelId);

        Assert.True(comparison.ModelAParameters >= comparison.ModelBParameters);
        Assert.True(comparison.CompressionRatio >= 1d);
    }

    [Fact(Skip = "SQL Server CLR assembly not redeployed - payload column still returns VECTOR type")]
    public async Task TextGeneration_PersistsProvenanceStream()
    {
        var prompt = $"integration provenance {Guid.NewGuid():N}";

        await using var connection = new SqlConnection(Fixture.ConnectionString);
        await connection.OpenAsync();

        Guid streamId;
        await using (var command = new SqlCommand("dbo.sp_GenerateText", connection)
        {
            CommandType = CommandType.StoredProcedure
        })
        {
            command.Parameters.Add(new SqlParameter("@prompt", SqlDbType.NVarChar, -1) { Value = prompt });
            command.Parameters.Add(new SqlParameter("@max_tokens", SqlDbType.Int) { Value = 4 });
            command.Parameters.Add(new SqlParameter("@temperature", SqlDbType.Float) { Value = 0.6 });
            command.Parameters.Add(new SqlParameter("@ModelIds", SqlDbType.NVarChar, -1) { Value = DBNull.Value });
            command.Parameters.Add(new SqlParameter("@top_k", SqlDbType.Int) { Value = 4 });

            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            streamId = reader.GetGuid(reader.GetOrdinal("StreamId"));
            Assert.NotEqual(Guid.Empty, streamId);
            Assert.True(reader.GetInt64(reader.GetOrdinal("InferenceId")) > 0);

            while (await reader.NextResultAsync())
            {
            }
        }

        var segments = new List<(string Kind, string? ContentType, string? Metadata, byte[] Payload)>();
        await using (var segmentCommand = new SqlCommand(@"
SELECT
    seg.segment_ordinal,
    seg.segment_kind,
    seg.content_type,
    seg.metadata,
    CAST(seg.payload AS VARBINARY(MAX)) AS payload
FROM provenance.GenerationStreams AS gs
CROSS APPLY provenance.clr_AtomicStreamSegments(gs.Stream) AS seg
WHERE gs.StreamId = @streamId
ORDER BY seg.segment_ordinal;", connection))
        {
            segmentCommand.Parameters.Add(new SqlParameter("@streamId", SqlDbType.UniqueIdentifier) { Value = streamId });

            await using var segmentReader = await segmentCommand.ExecuteReaderAsync();
            while (await segmentReader.ReadAsync())
            {
                var kind = segmentReader.GetString(segmentReader.GetOrdinal("segment_kind"));
                var contentType = segmentReader.IsDBNull(segmentReader.GetOrdinal("content_type"))
                    ? null
                    : segmentReader.GetString(segmentReader.GetOrdinal("content_type"));
                var metadata = segmentReader.IsDBNull(segmentReader.GetOrdinal("metadata"))
                    ? null
                    : segmentReader.GetString(segmentReader.GetOrdinal("metadata"));
                var payload = segmentReader.IsDBNull(segmentReader.GetOrdinal("payload"))
                    ? Array.Empty<byte>()
                    : (byte[])segmentReader["payload"];

                segments.Add((kind, contentType, metadata, payload));
            }
        }

        Assert.True(segments.Count >= 3);

        var inputSegment = Assert.Single(segments, s => s.Kind.Equals("Input", StringComparison.OrdinalIgnoreCase));
        var outputSegment = Assert.Single(segments, s => s.Kind.Equals("Output", StringComparison.OrdinalIgnoreCase));
        var telemetrySegment = Assert.Single(segments, s => s.Kind.Equals("Telemetry", StringComparison.OrdinalIgnoreCase));

        var promptText = Encoding.Unicode.GetString(inputSegment.Payload);
        Assert.Contains(prompt, promptText, StringComparison.Ordinal);

        var completion = Encoding.Unicode.GetString(outputSegment.Payload);
        Assert.False(string.IsNullOrWhiteSpace(completion));

        Assert.Contains("duration_ms", telemetrySegment.Metadata ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        await using var streamCommand = new SqlCommand(@"
SELECT Scope, Model, PayloadSizeBytes
FROM provenance.GenerationStreams
WHERE StreamId = @streamId;", connection);
        streamCommand.Parameters.Add(new SqlParameter("@streamId", SqlDbType.UniqueIdentifier) { Value = streamId });

        await using var streamReader = await streamCommand.ExecuteReaderAsync();
        Assert.True(await streamReader.ReadAsync());
        Assert.Equal("text_generation", streamReader.GetString(streamReader.GetOrdinal("Scope")));
        Assert.True(streamReader.GetInt64(streamReader.GetOrdinal("PayloadSizeBytes")) > 0);
    }
}




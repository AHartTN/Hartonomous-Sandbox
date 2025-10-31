using System.Linq;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Utilities;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Integration.Tests;

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
            .Include(e => e.Components)
            .Include(e => e.Atom)
            .Where(e => e.EmbeddingVector != null)
            .OrderBy(e => e.AtomEmbeddingId)
            .FirstOrDefaultAsync();

        Assert.NotNull(sample);

    Assert.NotNull(sample.EmbeddingVector);
    var vector = VectorUtility.Materialize(sample!.EmbeddingVector!.Value, sample.Dimension);
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
            .Include(e => e.Components)
            .Where(e => e.EmbeddingVector != null)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync();

        Assert.NotNull(sample);

    Assert.NotNull(sample.EmbeddingVector);
    var queryVector = VectorUtility.Materialize(sample!.EmbeddingVector!.Value, sample.Dimension);
        Assert.NotEmpty(queryVector);

        var results = await Fixture.InferenceService!
            .HybridSearchAsync(queryVector, topK: 5, candidateCount: 32);

        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task ComputeSpatialProjection_ProducesGeometry()
    {
        var sample = await Fixture.DbContext!.AtomEmbeddings
            .Include(e => e.Components)
            .Where(e => e.EmbeddingVector != null)
            .OrderBy(e => e.AtomEmbeddingId)
            .FirstOrDefaultAsync();

        Assert.NotNull(sample);

    Assert.NotNull(sample.EmbeddingVector);
    var dense = VectorUtility.Materialize(sample!.EmbeddingVector!.Value, sample.Dimension);
        var padded = VectorUtility.PadToSqlLength(dense, out _);
        var spatialPoint = await Fixture.AtomEmbeddings!
            .ComputeSpatialProjectionAsync(new Microsoft.Data.SqlTypes.SqlVector<float>(padded), dense.Length);

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

        var spatial = sample.SpatialGeometry!;
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
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Models;
using Hartonomous.Core.Utilities;
using Hartonomous.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAtomEmbeddingRepository"/> that layers centralized SQL execution
/// on top of EF Core tracking to support VECTOR-aware operations.
/// Inherits base CRUD from EfRepository, adds specialized vector/spatial search.
/// </summary>
public class AtomEmbeddingRepository : EfRepository<AtomEmbedding, long>, IAtomEmbeddingRepository
{
    private readonly ISqlCommandExecutor _sqlCommandExecutor;

    public AtomEmbeddingRepository(
        HartonomousDbContext context,
        ILogger<AtomEmbeddingRepository> logger,
        ISqlCommandExecutor sqlCommandExecutor)
        : base(context, logger)
    {
        _sqlCommandExecutor = sqlCommandExecutor ?? throw new ArgumentNullException(nameof(sqlCommandExecutor));
    }

    /// <summary>
    /// AtomEmbeddings are identified by AtomEmbeddingId property.
    /// </summary>
    protected override Expression<Func<AtomEmbedding, long>> GetIdExpression() => e => e.AtomEmbeddingId;

    /// <summary>
    /// Include components for complete embedding queries.
    /// </summary>
    protected override IQueryable<AtomEmbedding> IncludeRelatedEntities(IQueryable<AtomEmbedding> query)
    {
        return query.Include(e => e.Components);
    }

    // Domain-specific queries

    public async Task<IReadOnlyList<AtomEmbedding>> GetByAtomIdAsync(long atomId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(e => e.AtomId == atomId)
            .Include(e => e.Components)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Efficiently replace all components for an embedding using ExecuteDeleteAsync.
    /// Uses transaction for atomicity.
    /// </summary>
    public async Task AddComponentsAsync(long atomEmbeddingId, IEnumerable<AtomEmbeddingComponent> components, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(components);

        await using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await Context.AtomEmbeddingComponents
                .Where(c => c.AtomEmbeddingId == atomEmbeddingId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            await Context.AtomEmbeddingComponents.AddRangeAsync(components, cancellationToken).ConfigureAwait(false);
            await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    // Specialized vector/spatial operations (preserved - require raw SQL/stored procedures)

    public async Task<Point> ComputeSpatialProjectionAsync(SqlVector<float> paddedVector, int originalDimension, CancellationToken cancellationToken = default)
    {
        if (paddedVector.IsNull)
        {
            throw new ArgumentException("Projection requires a non-null SqlVector instance.", nameof(paddedVector));
        }

        VectorUtility.EnsureSupportedDimension(originalDimension);

        return await _sqlCommandExecutor.ExecuteAsync(async (command, token) =>
        {
            command.CommandText = "dbo.sp_ComputeSpatialProjection";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(new SqlParameter("@input_vector", paddedVector));
            command.Parameters.Add(new SqlParameter("@input_dimension", originalDimension));

            var xParameter = new SqlParameter("@output_x", SqlDbType.Float) { Direction = ParameterDirection.Output };
            var yParameter = new SqlParameter("@output_y", SqlDbType.Float) { Direction = ParameterDirection.Output };
            var zParameter = new SqlParameter("@output_z", SqlDbType.Float) { Direction = ParameterDirection.Output };

            command.Parameters.Add(xParameter);
            command.Parameters.Add(yParameter);
            command.Parameters.Add(zParameter);

            await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);

            static double ReadDouble(SqlParameter parameter)
                => parameter.Value is null or DBNull
                    ? 0d
                    : Convert.ToDouble(parameter.Value, CultureInfo.InvariantCulture);

            return new Point(new CoordinateZ(ReadDouble(xParameter), ReadDouble(yParameter), ReadDouble(zParameter)))
            {
                SRID = 0
            };
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AtomEmbeddingSearchResult?> FindNearestBySimilarityAsync(SqlVector<float> paddedVector, string embeddingType, int? modelId, double maxCosineDistance, CancellationToken cancellationToken = default)
    {
        if (paddedVector.IsNull)
        {
            throw new ArgumentException("Similarity search requires a non-null SqlVector instance.", nameof(paddedVector));
        }

        if (double.IsNaN(maxCosineDistance) || double.IsInfinity(maxCosineDistance))
        {
            return null;
        }

        var boundedDistance = Math.Clamp(maxCosineDistance, 0d, 2d);
        if (boundedDistance < 0d)
        {
            return null;
        }

        var searchResult = await _sqlCommandExecutor.ExecuteAsync(async (command, token) =>
        {
            const string sql = """
SELECT TOP (1)
        ae.AtomEmbeddingId,
        VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_vector) AS CosineDistance
FROM dbo.AtomEmbeddings ae
WHERE ae.EmbeddingVector IS NOT NULL
  AND (@embedding_type IS NULL OR ae.EmbeddingType = @embedding_type)
  AND (@model_id IS NULL OR ae.ModelId = @model_id)
    AND VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_vector) <= @max_distance
ORDER BY VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_vector);
""";

            command.CommandText = sql;
            command.CommandType = CommandType.Text;

            command.Parameters.Add(new SqlParameter("@query_vector", paddedVector));
            command.Parameters.Add(new SqlParameter("@embedding_type", SqlDbType.NVarChar, 128)
            {
                Value = string.IsNullOrWhiteSpace(embeddingType) ? DBNull.Value : embeddingType
            });
            command.Parameters.Add(new SqlParameter("@model_id", SqlDbType.Int)
            {
                Value = modelId.HasValue ? modelId.Value : DBNull.Value
            });
            command.Parameters.Add(new SqlParameter("@max_distance", SqlDbType.Float)
            {
                Value = boundedDistance
            });

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            if (!await reader.ReadAsync(token).ConfigureAwait(false))
            {
                return (EmbeddingId: (long?)null, CosineDistance: 0d);
            }

            return (EmbeddingId: reader.GetInt64(0), CosineDistance: reader.GetDouble(1));
        }, cancellationToken).ConfigureAwait(false);

        if (!searchResult.EmbeddingId.HasValue)
        {
            return null;
        }

        var embedding = await DbSet
            .Where(e => e.AtomEmbeddingId == searchResult.EmbeddingId.Value)
            .Include(e => e.Atom)
            .Include(e => e.Components)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (embedding is null)
        {
            return null;
        }

        return new AtomEmbeddingSearchResult
        {
            Embedding = embedding,
            CosineDistance = searchResult.CosineDistance,
            SpatialDistance = double.NaN
        };
    }

    public async Task<IReadOnlyList<AtomEmbeddingSearchResult>> HybridSearchAsync(float[] vector, Point spatial3D, int spatialCandidates, int finalTopK, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(vector);
        ArgumentNullException.ThrowIfNull(spatial3D);

        if (vector.Length == 0)
        {
            return Array.Empty<AtomEmbeddingSearchResult>();
        }

        VectorUtility.EnsureSupportedDimension(vector.Length);

        var candidateRecords = await _sqlCommandExecutor.ExecuteAsync(async (command, token) =>
        {
            const string sql = """
SELECT TOP (@candidateCount)
    ae.AtomEmbeddingId,
    ae.SpatialGeometry.STDistance(geometry::STGeomFromText(@wkt, @srid)) AS SpatialDistance
FROM dbo.AtomEmbeddings ae
WHERE ae.SpatialGeometry IS NOT NULL
ORDER BY ae.SpatialGeometry.STDistance(geometry::STGeomFromText(@wkt, @srid));
""";

            command.CommandText = sql;
            command.CommandType = CommandType.Text;

            var wkt = spatial3D.AsText();
            const string pointZPrefix = "POINT Z";
            if (wkt.StartsWith(pointZPrefix, StringComparison.OrdinalIgnoreCase))
            {
                wkt = "POINT" + wkt.Substring(pointZPrefix.Length);
            }

            command.Parameters.Add(new SqlParameter("@candidateCount", SqlDbType.Int) { Value = spatialCandidates });
            command.Parameters.Add(new SqlParameter("@wkt", SqlDbType.NVarChar, -1) { Value = wkt });
            command.Parameters.Add(new SqlParameter("@srid", SqlDbType.Int) { Value = spatial3D.SRID });

            var results = new List<(long EmbeddingId, double SpatialDistance)>();

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                var embeddingId = reader.GetInt64(0);
                var spatialDistance = reader.IsDBNull(1) ? double.MaxValue : reader.GetDouble(1);
                results.Add((embeddingId, spatialDistance));
            }

            return results;
        }, cancellationToken).ConfigureAwait(false);

        if (candidateRecords.Count == 0)
        {
            return Array.Empty<AtomEmbeddingSearchResult>();
        }

        var candidateIds = candidateRecords.Select(static c => c.EmbeddingId).ToArray();

        var embeddings = await DbSet
            .Where(e => candidateIds.Contains(e.AtomEmbeddingId))
            .Include(e => e.Components)
            .Include(e => e.Atom)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var embeddingLookup = embeddings.ToDictionary(static e => e.AtomEmbeddingId);
        var results = new List<AtomEmbeddingSearchResult>(embeddings.Count);
        ReadOnlySpan<float> queryVector = vector;

        foreach (var (embeddingId, spatialDistance) in candidateRecords)
        {
            if (!embeddingLookup.TryGetValue(embeddingId, out var embedding))
            {
                continue;
            }

            float[] candidateVector = embedding.EmbeddingVector is { IsNull: false } stored
                ? VectorUtility.Materialize(stored, embedding.Dimension)
                : VectorUtility.MaterializeFromComponents(embedding.Components, embedding.Dimension);

            if (candidateVector.Length == 0)
            {
                continue;
            }

            ReadOnlySpan<float> candidateSpan = candidateVector;
            var cosineDistance = VectorUtility.ComputeCosineDistance(queryVector, candidateSpan);

            results.Add(new AtomEmbeddingSearchResult
            {
                Embedding = embedding,
                CosineDistance = cosineDistance,
                SpatialDistance = spatialDistance
            });
        }

        return results
            .OrderBy(r => r.CosineDistance)
            .ThenBy(r => r.SpatialDistance)
            .Take(finalTopK)
            .ToList();
    }

    public Task UpdateSpatialMetadataAsync(long atomEmbeddingId, CancellationToken cancellationToken = default)
    {
        if (atomEmbeddingId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(atomEmbeddingId), atomEmbeddingId, "Embedding identifier must be positive.");
        }

        return _sqlCommandExecutor.ExecuteAsync(async (command, token) =>
        {
            command.CommandText = "dbo.sp_UpdateAtomEmbeddingSpatialMetadata";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(new SqlParameter("@embedding_id", SqlDbType.BigInt) { Value = atomEmbeddingId });

            await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        }, cancellationToken);
    }
}

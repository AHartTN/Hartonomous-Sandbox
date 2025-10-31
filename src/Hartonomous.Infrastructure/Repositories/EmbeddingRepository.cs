using System.Data;
using System.Data.Common;
using System.Globalization;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.ValueObjects;
using Hartonomous.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// Production-ready repository for working with embedding records stored via EF + raw SQL.
/// Provides CRUD operations, vector search, and spatial projection support.
/// </summary>
public sealed class EmbeddingRepository : IEmbeddingRepository
{
	private readonly HartonomousDbContext _context;
	private readonly ILogger<EmbeddingRepository> _logger;

	public EmbeddingRepository(HartonomousDbContext context, ILogger<EmbeddingRepository> logger)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<Embedding?> GetByIdAsync(long embeddingId, CancellationToken cancellationToken = default)
	{
		return await _context.Embeddings
			.AsNoTracking()
			.FirstOrDefaultAsync(e => e.EmbeddingId == embeddingId, cancellationToken)
			.ConfigureAwait(false);
	}

	public async Task<IEnumerable<Embedding>> GetBySourceTypeAsync(string sourceType, int limit = 100, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(sourceType))
		{
			throw new ArgumentException("Source type must be provided", nameof(sourceType));
		}

		return await _context.Embeddings
			.AsNoTracking()
			.Where(e => e.SourceType == sourceType)
			.OrderByDescending(e => e.CreatedAt)
			.Take(limit)
			.ToListAsync(cancellationToken)
			.ConfigureAwait(false);
	}

	public async Task<Embedding> AddAsync(Embedding embedding, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(embedding);

		await _context.Embeddings.AddAsync(embedding, cancellationToken).ConfigureAwait(false);
		await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

		return embedding;
	}

	public async Task<IEnumerable<Embedding>> AddRangeAsync(IEnumerable<Embedding> embeddings, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(embeddings);

		var buffer = embeddings.ToArray();
		if (buffer.Length == 0)
		{
			return Array.Empty<Embedding>();
		}

		await _context.Embeddings.AddRangeAsync(buffer, cancellationToken).ConfigureAwait(false);
		await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

		return buffer;
	}

	public async Task UpdateAsync(Embedding embedding, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(embedding);

		_context.Embeddings.Update(embedding);
		await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public async Task DeleteAsync(long embeddingId, CancellationToken cancellationToken = default)
	{
		var entity = await _context.Embeddings
			.FirstOrDefaultAsync(e => e.EmbeddingId == embeddingId, cancellationToken)
			.ConfigureAwait(false);

		if (entity is null)
		{
			return;
		}

		_context.Embeddings.Remove(entity);
		await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
	{
		return await _context.Embeddings.CountAsync(cancellationToken).ConfigureAwait(false);
	}

	public async Task<IEnumerable<EmbeddingSearchResult>> ExactSearchAsync(float[] queryVector, int topK = 10, string metric = "cosine", CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(queryVector);

		var results = new List<EmbeddingSearchResult>(Math.Max(topK, 1));

		using var command = CreateCommand();
		command.CommandText = @"
SELECT TOP (@top_k)
	EmbeddingId,
	SourceText,
	SourceType,
	VECTOR_DISTANCE(@metric, embedding_full, @vector) AS distance,
	CAST(1.0 - VECTOR_DISTANCE(@metric, embedding_full, @vector) AS FLOAT) AS similarity,
	CreatedAt,
	AccessCount
FROM dbo.Embeddings_Production WITH(READUNCOMMITTED)
WHERE embedding_full IS NOT NULL
ORDER BY VECTOR_DISTANCE(@metric, embedding_full, @vector);";

		AddParameter(command, "@vector", new SqlVector<float>(queryVector));
		AddParameter(command, "@metric", metric);
		AddParameter(command, "@top_k", topK);

		await ExecuteReaderAsync(command, reader =>
		{
			results.Add(new EmbeddingSearchResult
			{
				EmbeddingId = reader.GetInt64(0),
				SourceText = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
				SourceType = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
				Distance = Convert.ToSingle(reader.GetDouble(3)),
				SimilarityScore = Convert.ToSingle(reader.GetDouble(4)),
				CreatedTimestamp = reader.GetDateTime(5),
				ReferenceCount = reader.IsDBNull(6) ? null : reader.GetInt32(6)
			});
		}, cancellationToken).ConfigureAwait(false);

		return results;
	}

	public async Task<IEnumerable<EmbeddingSearchResult>> HybridSearchAsync(float[] queryVector, double queryX, double queryY, double queryZ, int spatialCandidates = 100, int finalTopK = 10, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(queryVector);

		var results = new List<EmbeddingSearchResult>(Math.Max(finalTopK, 1));

		using var command = CreateCommand();
		command.CommandText = @"
DECLARE @query_point GEOMETRY = geometry::STGeomFromText('POINT (' + CAST(@x AS NVARCHAR(32)) + ' ' + CAST(@y AS NVARCHAR(32)) + ' ' + CAST(@z AS NVARCHAR(32)) + ')', 0);

WITH spatial_candidates AS (
	SELECT TOP (@spatial_candidates)
		EmbeddingId,
		spatial_geometry.STDistance(@query_point) AS spatial_distance
	FROM dbo.Embeddings_Production WITH(INDEX(idx_spatial_fine))
	WHERE spatial_geometry IS NOT NULL
	ORDER BY spatial_geometry.STDistance(@query_point)
)
SELECT TOP (@final_top_k)
	ep.EmbeddingId,
	ep.SourceText,
	ep.SourceType,
	VECTOR_DISTANCE('cosine', ep.embedding_full, @vector) AS distance,
	CAST(1.0 - VECTOR_DISTANCE('cosine', ep.embedding_full, @vector) AS FLOAT) AS similarity,
	ep.CreatedAt,
	s.spatial_distance
FROM spatial_candidates s
JOIN dbo.Embeddings_Production ep ON ep.EmbeddingId = s.EmbeddingId
ORDER BY VECTOR_DISTANCE('cosine', ep.embedding_full, @vector);";

		AddParameter(command, "@vector", new SqlVector<float>(queryVector));
		AddParameter(command, "@x", queryX);
		AddParameter(command, "@y", queryY);
		AddParameter(command, "@z", queryZ);
		AddParameter(command, "@spatial_candidates", spatialCandidates);
		AddParameter(command, "@final_top_k", finalTopK);

		await ExecuteReaderAsync(command, reader =>
		{
			results.Add(new EmbeddingSearchResult
			{
				EmbeddingId = reader.GetInt64(0),
				SourceText = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
				SourceType = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
				Distance = Convert.ToSingle(reader.GetDouble(3)),
				SimilarityScore = Convert.ToSingle(reader.GetDouble(4)),
				CreatedTimestamp = reader.GetDateTime(5)
			});
		}, cancellationToken).ConfigureAwait(false);

		return results;
	}

	public async Task<Embedding?> CheckDuplicateByHashAsync(string contentHash, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(contentHash))
		{
			throw new ArgumentException("Content hash must be provided", nameof(contentHash));
		}

		return await _context.Embeddings
			.AsNoTracking()
			.FirstOrDefaultAsync(e => e.ContentHash == contentHash, cancellationToken)
			.ConfigureAwait(false);
	}

	public async Task<Embedding?> CheckDuplicateBySimilarityAsync(float[] queryVector, double threshold, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(queryVector);

		var maxDistance = 1.0 - Math.Clamp(threshold, -1.0, 1.0);

		using var command = CreateCommand();
		command.CommandText = @"
SELECT TOP (1)
	EmbeddingId,
	VECTOR_DISTANCE('cosine', embedding_full, @vector) AS distance
FROM dbo.Embeddings_Production WITH(READUNCOMMITTED)
WHERE embedding_full IS NOT NULL
ORDER BY VECTOR_DISTANCE('cosine', embedding_full, @vector);";

		AddParameter(command, "@vector", new SqlVector<float>(queryVector));

		Embedding? match = null;
		await ExecuteReaderAsync(command, async reader =>
		{
			var distance = reader.GetDouble(1);
			if (distance <= maxDistance)
			{
				var embeddingId = reader.GetInt64(0);
				match = await GetByIdAsync(embeddingId, cancellationToken).ConfigureAwait(false);
			}
		}, cancellationToken).ConfigureAwait(false);

		return match;
	}

	public async Task IncrementAccessCountAsync(long embeddingId, CancellationToken cancellationToken = default)
	{
		await _context.Database.ExecuteSqlInterpolatedAsync(
			$"UPDATE dbo.Embeddings_Production SET AccessCount = AccessCount + 1, LastAccessed = SYSUTCDATETIME() WHERE EmbeddingId = {embeddingId}",
			cancellationToken).ConfigureAwait(false);
	}

	public async Task<float[]> ComputeSpatialProjectionAsync(float[] fullVector, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(fullVector);

		using var command = CreateCommand();
		command.CommandText = "dbo.sp_ComputeSpatialProjection";
		command.CommandType = System.Data.CommandType.StoredProcedure;

		AddParameter(command, "@input_vector", new SqlVector<float>(fullVector));
		AddParameter(command, "@input_dimension", fullVector.Length);

		var xParam = new SqlParameter("@output_x", System.Data.SqlDbType.Float) { Direction = System.Data.ParameterDirection.Output };
		var yParam = new SqlParameter("@output_y", System.Data.SqlDbType.Float) { Direction = System.Data.ParameterDirection.Output };
		var zParam = new SqlParameter("@output_z", System.Data.SqlDbType.Float) { Direction = System.Data.ParameterDirection.Output };

		command.Parameters.Add(xParam);
		command.Parameters.Add(yParam);
		command.Parameters.Add(zParam);

		await ExecuteNonQueryAsync(command, cancellationToken).ConfigureAwait(false);

		return new[]
		{
			Convert.ToSingle(xParam.Value ?? 0f),
			Convert.ToSingle(yParam.Value ?? 0f),
			Convert.ToSingle(zParam.Value ?? 0f)
		};
	}

	public async Task<long> AddWithGeometryAsync(string sourceText, string sourceType, float[] embeddingFull, float[] spatial3D, string contentHash, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(embeddingFull);

		if (spatial3D is null || spatial3D.Length != 3)
		{
			throw new ArgumentException("Spatial projection must contain exactly three coordinates", nameof(spatial3D));
		}

		using var command = CreateCommand();
		command.CommandText = @"
INSERT INTO dbo.Embeddings_Production
(
	SourceText,
	SourceType,
	embedding_full,
	EmbeddingModel,
	SpatialProjX,
	SpatialProjY,
	SpatialProjZ,
	spatial_geometry,
	spatial_coarse,
	Dimension,
	ContentHash,
	CreatedAt,
	AccessCount
)
OUTPUT INSERTED.EmbeddingId
VALUES
(
	@source_text,
	@source_type,
	@vector,
	@model,
	@x,
	@y,
	@z,
	geometry::STGeomFromText(@fine_wkt, 0),
	geometry::STGeomFromText(@coarse_wkt, 0),
	@dimension,
	@content_hash,
	SYSUTCDATETIME(),
	1
);";

		AddParameter(command, "@source_text", (object?)sourceText ?? DBNull.Value);
		AddParameter(command, "@source_type", sourceType);
		AddParameter(command, "@vector", new SqlVector<float>(embeddingFull));
		AddParameter(command, "@model", "production");
		AddParameter(command, "@x", spatial3D[0]);
		AddParameter(command, "@y", spatial3D[1]);
		AddParameter(command, "@z", spatial3D[2]);
		AddParameter(command, "@fine_wkt", BuildPointWkt(spatial3D));
		AddParameter(command, "@coarse_wkt", BuildPointWkt(new[]
		{
			Math.Floor(spatial3D[0]),
			Math.Floor(spatial3D[1]),
			Math.Floor(spatial3D[2])
		}));
		AddParameter(command, "@dimension", embeddingFull.Length);
		AddParameter(command, "@content_hash", (object?)contentHash ?? DBNull.Value);

		try
		{
			var insertedId = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
			return Convert.ToInt64(insertedId, CultureInfo.InvariantCulture);
		}
		finally
		{
			command.Connection?.Close();
		}
	}

	private DbCommand CreateCommand()
	{
		var connection = _context.Database.GetDbConnection();
		if (connection.State != ConnectionState.Open)
		{
			connection.Open();
		}

		var command = connection.CreateCommand();
		command.CommandTimeout = 30;
		return command;
	}

	private static void AddParameter(DbCommand command, string name, object value)
	{
		var parameter = command.CreateParameter();
		parameter.ParameterName = name;
		parameter.Value = value ?? DBNull.Value;
		command.Parameters.Add(parameter);
	}

	private async Task ExecuteReaderAsync(DbCommand command, Action<DbDataReader> rowHandler, CancellationToken cancellationToken)
	{
		try
		{
			await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
			while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
			{
				rowHandler(reader);
			}
		}
		finally
		{
			command.Connection?.Close();
		}
	}

	private async Task ExecuteReaderAsync(DbCommand command, Func<DbDataReader, Task> rowHandler, CancellationToken cancellationToken)
	{
		try
		{
			await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
			while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
			{
				await rowHandler(reader).ConfigureAwait(false);
			}
		}
		finally
		{
			command.Connection?.Close();
		}
	}

	private async Task ExecuteNonQueryAsync(DbCommand command, CancellationToken cancellationToken)
	{
		try
		{
			await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			command.Connection?.Close();
		}
	}

	private static string BuildPointWkt(IReadOnlyList<float> coordinates)
	{
		var converted = new double[coordinates.Count];
		for (var i = 0; i < coordinates.Count; i++)
		{
			converted[i] = coordinates[i];
		}
		return BuildPointWkt(converted);
	}

	private static string BuildPointWkt(IReadOnlyList<double> coordinates)
	{
		return $"POINT Z ({coordinates[0]} {coordinates[1]} {coordinates[2]})";
	}
}

using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlTypes;
using Microsoft.Extensions.Logging;
using Hartonomous.Core.Interfaces;
using Hartonomous.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ModelIngestion
{
    /// <summary>
    /// Service for ingesting embeddings with dual representation:
    /// - Full VECTOR(384/768/1536) for exact similarity
    /// - GEOMETRY(3D) for fast approximate spatial queries
    /// - Content-addressable deduplication via SHA256 hashing
    /// </summary>
    public class EmbeddingIngestionService : IEmbeddingIngestionService
    {
        private readonly string _connectionString;
        private readonly string _embeddingModel;
        private readonly int _embeddingDimension;
        private readonly double _deduplicationThreshold;
        private readonly IEmbeddingRepository _embeddingRepository;
        private readonly ILogger<EmbeddingIngestionService> _logger;

        public EmbeddingIngestionService(
            IEmbeddingRepository embeddingRepository,
            ILogger<EmbeddingIngestionService> logger,
            string connectionString,
            string embeddingModel = "custom",
            int embeddingDimension = 768,
            double deduplicationThreshold = 0.95)
        {
            _embeddingRepository = embeddingRepository ?? throw new ArgumentNullException(nameof(embeddingRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionString = connectionString;
            _embeddingModel = embeddingModel;
            _embeddingDimension = embeddingDimension;
            _deduplicationThreshold = deduplicationThreshold;
        }

        /// <summary>
        /// Ingest embedding with deduplication:
        /// 1. Check content hash (SHA256 of source_text)
        /// 2. Check embedding similarity (cosine > threshold)
        /// 3. Skip if duplicate exists, update access_count
        /// 4. Insert if new
        /// </summary>
        public async Task<Hartonomous.Core.Interfaces.EmbeddingIngestionResult> IngestEmbeddingAsync(
            string sourceText,
            string sourceType,
            float[] embeddingFull,
            float[]? spatial3D = null,
            CancellationToken cancellationToken = default)
        {
            if (embeddingFull.Length != _embeddingDimension)
            {
                throw new ArgumentException(
                    $"Embedding dimension mismatch. Expected {_embeddingDimension}, got {embeddingFull.Length}");
            }

            // Step 1: Compute content hash (SHA256 as hex string)
            var contentHashString = ComputeSHA256HashString(sourceText);

            // Step 2: Check for exact content match using repository
            var existingByHash = await _embeddingRepository.CheckDuplicateByHashAsync(contentHashString, cancellationToken);
            if (existingByHash != null)
            {
                _logger.LogInformation("Found duplicate by hash: embedding_id={EmbeddingId}", existingByHash.EmbeddingId);
                await _embeddingRepository.IncrementAccessCountAsync(existingByHash.EmbeddingId, cancellationToken);
                
                return new Hartonomous.Core.Interfaces.EmbeddingIngestionResult
                {
                    EmbeddingId = existingByHash.EmbeddingId,
                    WasDuplicate = true,
                    DuplicateReason = "Exact content hash match"
                };
            }

            // Step 3: Check for semantic similarity using configured threshold
            // Default 0.95 (95% similar) catches paraphrases and near-duplicates
            var existingBySimilarity = await _embeddingRepository.CheckDuplicateBySimilarityAsync(
                embeddingFull, _deduplicationThreshold, cancellationToken);
            if (existingBySimilarity != null)
            {
                _logger.LogInformation("Found duplicate by similarity: embedding_id={EmbeddingId}, threshold={Threshold}", 
                    existingBySimilarity.EmbeddingId, _deduplicationThreshold);
                await _embeddingRepository.IncrementAccessCountAsync(existingBySimilarity.EmbeddingId, cancellationToken);
                
                return new Hartonomous.Core.Interfaces.EmbeddingIngestionResult
                {
                    EmbeddingId = existingBySimilarity.EmbeddingId,
                    WasDuplicate = true,
                    DuplicateReason = $"High semantic similarity (cosine > {_deduplicationThreshold:F2})"
                };
            }

            // Step 4: Compute spatial projection if not provided
            if (spatial3D == null)
            {
                spatial3D = await _embeddingRepository.ComputeSpatialProjectionAsync(embeddingFull, cancellationToken);
            }

            if (spatial3D.Length != 3)
            {
                throw new ArgumentException("Spatial projection must be 3D");
            }

            // Step 5: Insert new embedding (still needs direct SQL for GEOMETRY construction)
            var embeddingId = await InsertEmbeddingDirectAsync(
                sourceText, sourceType, embeddingFull, spatial3D, contentHashString, cancellationToken);

            _logger.LogInformation("Inserted new embedding: embedding_id={EmbeddingId}", embeddingId);
            
            return new Hartonomous.Core.Interfaces.EmbeddingIngestionResult
            {
                EmbeddingId = embeddingId,
                WasDuplicate = false,
                DuplicateReason = null
            };
        }

        /// <summary>
        /// Ingest multiple embeddings in a batch
        /// </summary>
        public async Task<IEnumerable<Hartonomous.Core.Interfaces.EmbeddingIngestionResult>> IngestBatchAsync(
            IEnumerable<(string sourceText, string sourceType, float[] embedding)> batch,
            CancellationToken cancellationToken = default)
        {
            var results = new List<Hartonomous.Core.Interfaces.EmbeddingIngestionResult>();
            
            foreach (var (sourceText, sourceType, embedding) in batch)
            {
                var result = await IngestEmbeddingAsync(sourceText, sourceType, embedding, null, cancellationToken);
                results.Add(result);
            }
            
            return results;
        }



        /// <summary>
        /// Insert new embedding with content hash for deduplication
        /// Uses SqlVector<float> for native VECTOR type support
        /// NOTE: Direct SQL needed because EF Core doesn't support GEOMETRY construction in queries
        /// </summary>
        private async Task<long> InsertEmbeddingDirectAsync(
            string sourceText,
            string sourceType,
            float[] embeddingFull,
            float[] spatial3D,
            string contentHashString,
            CancellationToken cancellationToken)
        {
            var sql = @"
                INSERT INTO dbo.Embeddings_Production (
                    source_text,
                    source_type,
                    embedding_full,
                    embedding_model,
                    spatial_proj_x,
                    spatial_proj_y,
                    spatial_proj_z,
                    spatial_geometry,
                    spatial_coarse,
                    dimension,
                    content_hash,
                    access_count
                ) VALUES (
                    @source_text,
                    @source_type,
                    @embedding_full,
                    @embedding_model,
                    @x, @y, @z,
                    geometry::STGeomFromText('POINT(' +
                        CAST(@x AS NVARCHAR(50)) + ' ' +
                        CAST(@y AS NVARCHAR(50)) + ')', 0),
                    geometry::STGeomFromText('POINT(' +
                        CAST(FLOOR(@x) AS NVARCHAR(50)) + ' ' +
                        CAST(FLOOR(@y) AS NVARCHAR(50)) + ')', 0),
                    @dimension,
                    @content_hash,
                    1
                );
                SELECT SCOPE_IDENTITY();
            ";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@source_text", sourceText);
            cmd.Parameters.AddWithValue("@source_type", sourceType);
            
            // Use SqlVector<float> - Microsoft.Data.SqlClient 6.1.2+ handles this natively
            cmd.Parameters.AddWithValue("@embedding_full", new SqlVector<float>(embeddingFull));
            
            cmd.Parameters.AddWithValue("@embedding_model", _embeddingModel);
            cmd.Parameters.AddWithValue("@x", spatial3D[0]);
            cmd.Parameters.AddWithValue("@y", spatial3D[1]);
            cmd.Parameters.AddWithValue("@z", spatial3D[2]);
            cmd.Parameters.AddWithValue("@dimension", embeddingFull.Length);
            cmd.Parameters.AddWithValue("@content_hash", contentHashString);

            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result);
        }

        /// <summary>
        /// Compute SHA256 hash of content and return as hex string
        /// </summary>
        private string ComputeSHA256HashString(string content)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(content);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }

        /// <summary>
        /// Ingest embeddings with dual representation (legacy method, no deduplication)
        /// Uses SqlVector<float> for native VECTOR type support
        /// </summary>
        public async Task<long> IngestEmbeddingAsync(
            string sourceText,
            string sourceType,
            float[] embeddingFull,
            float[] spatial3D)
        {
            if (embeddingFull.Length != _embeddingDimension)
            {
                throw new ArgumentException(
                    $"Embedding dimension mismatch. Expected {_embeddingDimension}, got {embeddingFull.Length}");
            }

            if (spatial3D.Length != 3)
            {
                throw new ArgumentException("Spatial projection must be 3D");
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                INSERT INTO dbo.Embeddings_Production (
                    source_text,
                    source_type,
                    embedding_full,
                    embedding_model,
                    spatial_proj_x,
                    spatial_proj_y,
                    spatial_proj_z,
                    spatial_geometry,
                    spatial_coarse,
                    dimension
                ) VALUES (
                    @source_text,
                    @source_type,
                    @embedding_full,
                    @embedding_model,
                    @x, @y, @z,
                    geometry::STGeomFromText('POINT(' +
                        CAST(@x AS NVARCHAR(50)) + ' ' +
                        CAST(@y AS NVARCHAR(50)) + ')', 0),
                    geometry::STGeomFromText('POINT(' +
                        CAST(FLOOR(@x) AS NVARCHAR(50)) + ' ' +
                        CAST(FLOOR(@y) AS NVARCHAR(50)) + ')', 0),
                    @dimension
                );
                SELECT SCOPE_IDENTITY();
            ";

            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@source_text", sourceText);
            cmd.Parameters.AddWithValue("@source_type", sourceType);
            
            // Use SqlVector<float> - Microsoft.Data.SqlClient 6.1.2+ handles this natively
            cmd.Parameters.AddWithValue("@embedding_full", new SqlVector<float>(embeddingFull));
            
            cmd.Parameters.AddWithValue("@embedding_model", _embeddingModel);
            cmd.Parameters.AddWithValue("@x", spatial3D[0]);
            cmd.Parameters.AddWithValue("@y", spatial3D[1]);
            cmd.Parameters.AddWithValue("@z", spatial3D[2]);
            cmd.Parameters.AddWithValue("@dimension", _embeddingDimension);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result);
        }

        /// <summary>
        /// Batch ingest multiple embeddings efficiently
        /// </summary>
        public async Task<int> IngestEmbeddingBatchAsync(List<EmbeddingData> embeddings)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            int count = 0;
            try
            {
                foreach (var embedding in embeddings)
                {
                    await IngestSingleEmbeddingAsync(
                        connection, transaction, embedding);
                    count++;

                    if (count % 100 == 0)
                    {
                        Console.WriteLine($"  Ingested {count}/{embeddings.Count} embeddings");
                    }
                }

                await transaction.CommitAsync();
                Console.WriteLine($"✓ Successfully ingested {count} embeddings");
                return count;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"✗ Error during batch ingestion: {ex.Message}");
                throw;
            }
        }

        private async Task IngestSingleEmbeddingAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            EmbeddingData embedding)
        {
            var sql = @"
                INSERT INTO dbo.Embeddings_Production (
                    source_text, source_type, embedding_full, embedding_model,
                    spatial_proj_x, spatial_proj_y, spatial_proj_z,
                    spatial_geometry, spatial_coarse, dimension
                ) VALUES (
                    @source_text, @source_type,
                    @embedding_full,
                    @embedding_model,
                    @x, @y, @z,
                    geometry::STGeomFromText('POINT(' +
                        CAST(@x AS NVARCHAR(50)) + ' ' +
                        CAST(@y AS NVARCHAR(50)) + ')', 0),
                    geometry::STGeomFromText('POINT(' +
                        CAST(FLOOR(@x) AS NVARCHAR(50)) + ' ' +
                        CAST(FLOOR(@y) AS NVARCHAR(50)) + ')', 0),
                    @dimension
                );
            ";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.Parameters.AddWithValue("@source_text", embedding.SourceText);
            cmd.Parameters.AddWithValue("@source_type", embedding.SourceType);
            
            // Use SqlVector<float> - Microsoft.Data.SqlClient 6.1.2+ handles this natively
            cmd.Parameters.AddWithValue("@embedding_full", new SqlVector<float>(embedding.EmbeddingFull));
            
            cmd.Parameters.AddWithValue("@embedding_model", embedding.EmbeddingModel ?? _embeddingModel);
            cmd.Parameters.AddWithValue("@x", embedding.Spatial3D[0]);
            cmd.Parameters.AddWithValue("@y", embedding.Spatial3D[1]);
            cmd.Parameters.AddWithValue("@z", embedding.Spatial3D[2]);
            cmd.Parameters.AddWithValue("@dimension", embedding.EmbeddingFull.Length);

            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Compute 3D spatial projection using distance-based coordinates
        /// Delegates to T-SQL stored procedure for pure SQL Server implementation
        /// Uses SqlVector<float> for native VECTOR type support
        /// </summary>
        public async Task<float[]> ComputeSpatialProjectionAsync(float[] fullVector)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Note: sp_ComputeSpatialProjection expects VECTOR parameter
            var sql = @"
                DECLARE @x FLOAT, @y FLOAT, @z FLOAT;
                EXEC dbo.sp_ComputeSpatialProjection
                    @input_vector = @vector,
                    @output_x = @x OUTPUT,
                    @output_y = @y OUTPUT,
                    @output_z = @z OUTPUT;
                SELECT @x AS x, @y AS y, @z AS z;
            ";

            using var cmd = new SqlCommand(sql, connection);
            
            // Use SqlVector<float> - Microsoft.Data.SqlClient 6.1.2+ handles this natively
            cmd.Parameters.AddWithValue("@vector", new SqlVector<float>(fullVector));
            
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new[]
                {
                    reader.GetFloat(0),
                    reader.GetFloat(1),
                    reader.GetFloat(2)
                };
            }

            throw new InvalidOperationException("Spatial projection failed: no output returned");
        }

        /// <summary>
        /// Query embeddings using exact VECTOR search
        /// Uses SqlVector<float> for native VECTOR type support
        /// </summary>
        public async Task<List<SearchResult>> ExactSearchAsync(
            float[] queryVector,
            int topK = 10,
            string distanceMetric = "cosine")
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT TOP (@top_k)
                    embedding_id,
                    source_text,
                    source_type,
                    VECTOR_DISTANCE(@metric, embedding_full, @query) as distance
                FROM dbo.Embeddings_Production
                WHERE embedding_full IS NOT NULL
                ORDER BY VECTOR_DISTANCE(@metric, embedding_full, @query);
            ";

            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@top_k", topK);
            cmd.Parameters.AddWithValue("@metric", distanceMetric);
            
            // Use SqlVector<float> - Microsoft.Data.SqlClient 6.1.2+ handles this natively
            cmd.Parameters.AddWithValue("@query", new SqlVector<float>(queryVector));

            var results = new List<SearchResult>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new SearchResult
                {
                    EmbeddingId = reader.GetInt64(0),
                    SourceText = reader.GetString(1),
                    SourceType = reader.GetString(2),
                    Distance = reader.GetDouble(3)
                });
            }

            return results;
        }

        /// <summary>
        /// Query embeddings using approximate spatial search
        /// </summary>
        public async Task<List<SearchResult>> ApproxSearchAsync(
            float[] spatial3D,
            int topK = 10,
            bool useCoarse = false)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var geomColumn = useCoarse ? "spatial_coarse" : "spatial_geometry";
            var sql = $@"
                DECLARE @query_pt GEOMETRY = geometry::STGeomFromText(
                    'POINT(' + CAST(@x AS NVARCHAR(50)) + ' ' +
                              CAST(@y AS NVARCHAR(50)) + ')', 0);

                SELECT TOP (@top_k)
                    embedding_id,
                    source_text,
                    source_type,
                    {geomColumn}.STDistance(@query_pt) as spatial_distance
                FROM dbo.Embeddings_Production
                WHERE {geomColumn} IS NOT NULL
                ORDER BY {geomColumn}.STDistance(@query_pt);
            ";

            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@top_k", topK);
            cmd.Parameters.AddWithValue("@x", spatial3D[0]);
            cmd.Parameters.AddWithValue("@y", spatial3D[1]);

            var results = new List<SearchResult>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new SearchResult
                {
                    EmbeddingId = reader.GetInt64(0),
                    SourceText = reader.GetString(1),
                    SourceType = reader.GetString(2),
                    Distance = reader.GetDouble(3)
                });
            }

            return results;
        }
    }

    public class EmbeddingData
    {
        public string SourceText { get; set; } = "";
        public string SourceType { get; set; } = "sentence";
        public float[] EmbeddingFull { get; set; } = Array.Empty<float>();
        public float[] Spatial3D { get; set; } = Array.Empty<float>();
        public string? EmbeddingModel { get; set; }
    }

    public class SearchResult
    {
        public long EmbeddingId { get; set; }
        public string SourceText { get; set; } = "";
        public string SourceType { get; set; } = "";
        public double Distance { get; set; }
    }

    public class EmbeddingIngestionResult
    {
        public long EmbeddingId { get; set; }
        public bool WasDuplicate { get; set; }
        public string? DuplicateReason { get; set; }
    }
}

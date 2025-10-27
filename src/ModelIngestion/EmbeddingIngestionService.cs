using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ModelIngestion
{
    /// <summary>
    /// Service for ingesting embeddings with dual representation:
    /// - Full VECTOR(384/768/1536) for exact similarity
    /// - GEOMETRY(3D) for fast approximate spatial queries
    /// - Content-addressable deduplication via SHA256 hashing
    /// </summary>
    public class EmbeddingIngestionService
    {
        private readonly string _connectionString;
        private readonly string _embeddingModel;
        private readonly int _embeddingDimension;
        private readonly double _deduplicationThreshold;

        public EmbeddingIngestionService(
            string connectionString,
            string embeddingModel = "custom",
            int embeddingDimension = 768,
            double deduplicationThreshold = 0.95)
        {
            _connectionString = connectionString;
            _embeddingModel = embeddingModel;
            _embeddingDimension = embeddingDimension;
            _deduplicationThreshold = deduplicationThreshold;
        }

        /// <summary>
        /// Ingest embedding with deduplication:
        /// 1. Check content hash (SHA256 of source_text)
        /// 2. Check embedding similarity (cosine > 0.99)
        /// 3. Skip if duplicate exists, update access_count
        /// 4. Insert if new
        /// </summary>
        public async Task<EmbeddingIngestionResult> IngestEmbeddingWithDeduplicationAsync(
            string sourceText,
            string sourceType,
            float[] embeddingFull,
            float[]? spatial3D = null)
        {
            if (embeddingFull.Length != _embeddingDimension)
            {
                throw new ArgumentException(
                    $"Embedding dimension mismatch. Expected {_embeddingDimension}, got {embeddingFull.Length}");
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Step 1: Compute content hash
            var contentHash = ComputeSHA256Hash(sourceText);

            // Step 2: Check for exact content match
            var existingByHash = await CheckDuplicateByHashAsync(connection, contentHash);
            if (existingByHash.HasValue)
            {
                await IncrementAccessCountAsync(connection, existingByHash.Value);
                return new EmbeddingIngestionResult
                {
                    EmbeddingId = existingByHash.Value,
                    WasDuplicate = true,
                    DuplicateReason = "Exact content hash match"
                };
            }

            // Step 3: Check for semantic similarity using configured threshold
            // Default 0.95 (95% similar) catches paraphrases and near-duplicates
            // Adjustable based on use case: stricter (0.97) or looser (0.90)
            var existingBySimilarity = await CheckDuplicateBySimilarityAsync(
                connection, embeddingFull, _deduplicationThreshold);
            if (existingBySimilarity.HasValue)
            {
                await IncrementAccessCountAsync(connection, existingBySimilarity.Value);
                return new EmbeddingIngestionResult
                {
                    EmbeddingId = existingBySimilarity.Value,
                    WasDuplicate = true,
                    DuplicateReason = $"High semantic similarity (cosine > {_deduplicationThreshold:F2})"
                };
            }

            // Step 4: Compute spatial projection if not provided
            if (spatial3D == null)
            {
                spatial3D = await ComputeSpatialProjectionAsync(embeddingFull);
            }

            if (spatial3D.Length != 3)
            {
                throw new ArgumentException("Spatial projection must be 3D");
            }

            // Step 5: Insert new embedding with content hash
            var embeddingId = await InsertEmbeddingAsync(
                connection, sourceText, sourceType, embeddingFull, spatial3D, contentHash);

            return new EmbeddingIngestionResult
            {
                EmbeddingId = embeddingId,
                WasDuplicate = false,
                DuplicateReason = null
            };
        }

        /// <summary>
        /// Check if content hash already exists in database
        /// </summary>
        private async Task<long?> CheckDuplicateByHashAsync(
            SqlConnection connection,
            byte[] contentHash)
        {
            var sql = @"
                SELECT TOP 1 embedding_id
                FROM dbo.Embeddings_Production
                WHERE content_hash = @hash;
            ";

            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@hash", contentHash);

            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt64(result) : null;
        }

        /// <summary>
        /// Check if semantically similar embedding exists (cosine similarity > threshold)
        /// Uses SqlVector<float> for native VECTOR type support
        /// </summary>
        private async Task<long?> CheckDuplicateBySimilarityAsync(
            SqlConnection connection,
            float[] queryVector,
            double threshold)
        {
            var sql = @"
                SELECT TOP 1 embedding_id
                FROM dbo.Embeddings_Production
                WHERE embedding_full IS NOT NULL
                  AND VECTOR_DISTANCE('cosine', embedding_full, @query) < @threshold
                ORDER BY VECTOR_DISTANCE('cosine', embedding_full, @query);
            ";

            using var cmd = new SqlCommand(sql, connection);
            
            // Use SqlVector<float> - exact pattern from Microsoft documentation
            cmd.Parameters.AddWithValue("@query", new SqlVector<float>(queryVector));
            cmd.Parameters.AddWithValue("@threshold", 1.0 - threshold); // Cosine distance = 1 - similarity

            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt64(result) : null;
        }

        /// <summary>
        /// Increment access_count for existing embedding (deduplication tracking)
        /// </summary>
        private async Task IncrementAccessCountAsync(SqlConnection connection, long embeddingId)
        {
            var sql = @"
                UPDATE dbo.Embeddings_Production
                SET access_count = access_count + 1,
                    last_accessed = SYSUTCDATETIME()
                WHERE embedding_id = @id;
            ";

            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@id", embeddingId);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Insert new embedding with content hash for deduplication
        /// Uses SqlVector<float> for native VECTOR type support
        /// </summary>
        private async Task<long> InsertEmbeddingAsync(
            SqlConnection connection,
            string sourceText,
            string sourceType,
            float[] embeddingFull,
            float[] spatial3D,
            byte[] contentHash)
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
            cmd.Parameters.AddWithValue("@content_hash", contentHash);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result);
        }

        /// <summary>
        /// Compute SHA256 hash of content for deduplication
        /// </summary>
        private byte[] ComputeSHA256Hash(string content)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(content);
            return sha256.ComputeHash(bytes);
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

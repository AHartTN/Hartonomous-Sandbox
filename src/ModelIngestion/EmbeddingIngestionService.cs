using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Hartonomous.Core.Interfaces;
using Hartonomous.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
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
        private readonly string _embeddingModel;
        private readonly int _embeddingDimension;
        private readonly double _deduplicationThreshold;
        private readonly IEmbeddingRepository _embeddingRepository;
        private readonly ILogger<EmbeddingIngestionService> _logger;

        public EmbeddingIngestionService(
            IEmbeddingRepository embeddingRepository,
            ILogger<EmbeddingIngestionService> logger,
            IConfiguration configuration)
        {
            _embeddingRepository = embeddingRepository ?? throw new ArgumentNullException(nameof(embeddingRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Get configuration values
            _embeddingModel = configuration.GetValue<string>("Ingestion:EmbeddingModel", "production");
            _embeddingDimension = configuration.GetValue<int>("Ingestion:EmbeddingDimension", 768);
            _deduplicationThreshold = configuration.GetValue<double>("Ingestion:DeduplicationThreshold", 0.95);
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

            // Step 5: Insert new embedding using repository
            var embeddingId = await _embeddingRepository.AddWithGeometryAsync(
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
        /// Compute SHA256 hash of content and return as hex string
        /// </summary>
        private string ComputeSHA256HashString(string content)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(content);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }
}

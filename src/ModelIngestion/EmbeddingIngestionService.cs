using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Services;
using Hartonomous.Core.Utilities;
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
    /// Now inherits from BaseConfigurableService for better structure.
    /// </summary>
    public class EmbeddingIngestionService : BaseConfigurableService<EmbeddingIngestionConfig>, IEmbeddingIngestionService
    {
        private readonly IEmbeddingRepository _embeddingRepository;

        public EmbeddingIngestionService(
            IEmbeddingRepository embeddingRepository,
            ILogger<EmbeddingIngestionService> logger,
            IConfiguration configuration)
            : base(logger, new EmbeddingIngestionConfig(configuration))
        {
            _embeddingRepository = embeddingRepository ?? throw new ArgumentNullException(nameof(embeddingRepository));
        }

        public override string ServiceName => "EmbeddingIngestionService";

        public async Task<Hartonomous.Core.Interfaces.EmbeddingIngestionResult> IngestEmbeddingAsync(
            string sourceText,
            string sourceType,
            float[] embeddingFull,
            float[]? spatial3D = null,
            CancellationToken cancellationToken = default)
        {
            if (embeddingFull.Length != Config.EmbeddingDimension)
            {
                throw new ArgumentException(
                    $"Embedding dimension mismatch. Expected {Config.EmbeddingDimension}, got {embeddingFull.Length}");
            }

            // Step 1: Compute content hash (SHA256 as hex string)
            var contentHashString = HashUtility.ComputeSHA256Hash(sourceText);

            // Step 2: Check for exact content match using repository
            var existingByHash = await _embeddingRepository.CheckDuplicateByHashAsync(contentHashString, cancellationToken);
            if (existingByHash != null)
            {
                Logger.LogInformation("Found duplicate by hash: embedding_id={EmbeddingId}", existingByHash.EmbeddingId);
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
                embeddingFull, Config.DeduplicationThreshold, cancellationToken);
            if (existingBySimilarity != null)
            {
                Logger.LogInformation("Found duplicate by similarity: embedding_id={EmbeddingId}, threshold={Threshold}",
                    existingBySimilarity.EmbeddingId, Config.DeduplicationThreshold);
                await _embeddingRepository.IncrementAccessCountAsync(existingBySimilarity.EmbeddingId, cancellationToken);
                
                return new Hartonomous.Core.Interfaces.EmbeddingIngestionResult
                {
                    EmbeddingId = existingBySimilarity.EmbeddingId,
                    WasDuplicate = true,
                    DuplicateReason = $"High semantic similarity (cosine > {Config.DeduplicationThreshold:F2})"
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

            Logger.LogInformation("Inserted new embedding: embedding_id={EmbeddingId}", embeddingId);
            
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
            return HashUtility.ComputeSHA256Hash(content);
        }
    }

    /// <summary>
    /// Configuration for embedding ingestion service.
    /// </summary>
    public class EmbeddingIngestionConfig
    {
        public string EmbeddingModel { get; set; }
        public int EmbeddingDimension { get; set; }
        public double DeduplicationThreshold { get; set; }

        public EmbeddingIngestionConfig(IConfiguration configuration)
        {
            EmbeddingModel = configuration.GetValue<string>("Ingestion:EmbeddingModel", "production");
            EmbeddingDimension = configuration.GetValue<int>("Ingestion:EmbeddingDimension", 768);
            DeduplicationThreshold = configuration.GetValue<double>("Ingestion:DeduplicationThreshold", 0.95);
        }
    }
}

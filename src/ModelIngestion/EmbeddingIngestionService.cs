using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
        private readonly IAtomIngestionService _atomIngestionService;

        public EmbeddingIngestionService(
            IAtomIngestionService atomIngestionService,
            ILogger<EmbeddingIngestionService> logger,
            IConfiguration configuration)
            : base(logger, new EmbeddingIngestionConfig(configuration))
        {
            _atomIngestionService = atomIngestionService ?? throw new ArgumentNullException(nameof(atomIngestionService));
        }

        public override string ServiceName => "EmbeddingIngestionService";

    public async Task<EmbeddingIngestionResult> IngestEmbeddingAsync(
            string sourceText,
            string sourceType,
            float[] embeddingFull,
            float[]? spatial3D = null,
            CancellationToken cancellationToken = default)
        {
            if (embeddingFull is null)
            {
                throw new ArgumentNullException(nameof(embeddingFull));
            }

            if (embeddingFull.Length != Config.EmbeddingDimension)
            {
                throw new ArgumentException(
                    $"Embedding dimension mismatch. Expected {Config.EmbeddingDimension}, got {embeddingFull.Length}");
            }

            var modality = string.IsNullOrWhiteSpace(Config.DefaultModality) ? "text" : Config.DefaultModality;
            var policyName = string.IsNullOrWhiteSpace(Config.DeduplicationPolicy) ? "default" : Config.DeduplicationPolicy;

            var atomRequest = new AtomIngestionRequest
            {
                HashInput = sourceText,
                Modality = modality,
                Subtype = sourceType,
                SourceType = sourceType,
                CanonicalText = sourceText,
                Metadata = null,
                Embedding = embeddingFull,
                EmbeddingType = Config.EmbeddingModel,
                ModelId = Config.DefaultModelId,
                PolicyName = policyName
            };

            var atomResult = await _atomIngestionService
                .IngestAsync(atomRequest, cancellationToken)
                .ConfigureAwait(false);

            if (atomResult.WasDuplicate)
            {
                Logger.LogInformation(
                    "Reused existing atom {AtomId} for embedding (Reason: {Reason})",
                    atomResult.Atom.AtomId,
                    atomResult.DuplicateReason ?? "semantic match");
            }
            else
            {
                Logger.LogInformation(
                    "Stored new atom {AtomId} with embedding {EmbeddingId}",
                    atomResult.Atom.AtomId,
                    atomResult.Embedding?.AtomEmbeddingId);
            }

            return new EmbeddingIngestionResult
            {
                AtomId = atomResult.Atom.AtomId,
                AtomEmbeddingId = atomResult.Embedding?.AtomEmbeddingId,
                WasDuplicate = atomResult.WasDuplicate,
                DuplicateReason = atomResult.DuplicateReason,
                SemanticSimilarity = atomResult.SemanticSimilarity
            };
        }

        /// <summary>
        /// Ingest multiple embeddings in a batch
        /// </summary>
    public async Task<IEnumerable<EmbeddingIngestionResult>> IngestBatchAsync(
            IEnumerable<(string sourceText, string sourceType, float[] embedding)> batch,
            CancellationToken cancellationToken = default)
        {
            var results = new List<EmbeddingIngestionResult>();

            foreach (var (sourceText, sourceType, embedding) in batch)
            {
                var result = await IngestEmbeddingAsync(sourceText, sourceType, embedding, null, cancellationToken);
                results.Add(result);
            }

            return results;
        }


    }

    /// <summary>
    /// Configuration for embedding ingestion service.
    /// </summary>
    public class EmbeddingIngestionConfig
    {
        public string EmbeddingModel { get; }
        public int EmbeddingDimension { get; }
        public string DeduplicationPolicy { get; }
        public string DefaultModality { get; }
        public int? DefaultModelId { get; }

        public EmbeddingIngestionConfig(IConfiguration configuration)
        {
            EmbeddingModel = configuration.GetValue<string>("Ingestion:EmbeddingModel", "production");
            EmbeddingDimension = configuration.GetValue<int>("Ingestion:EmbeddingDimension", 768);
            DeduplicationPolicy = configuration.GetValue<string>("Ingestion:DeduplicationPolicy", "default");
            DefaultModality = configuration.GetValue<string>("Ingestion:DefaultModality", "text");
            DefaultModelId = configuration.GetValue<int?>("Ingestion:DefaultModelId");
        }
    }
}

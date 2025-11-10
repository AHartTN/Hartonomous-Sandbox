using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Services;
using Hartonomous.Infrastructure.Prediction;
using Hartonomous.Infrastructure.Services;
using Hartonomous.Infrastructure.Services.Autonomous;
using Hartonomous.Infrastructure.Services.ContentExtraction;
using Hartonomous.Infrastructure.Services.ContentExtraction.Extractors;
using Hartonomous.Infrastructure.Services.Enrichment;
using Hartonomous.Infrastructure.Services.Generation;
using Hartonomous.Infrastructure.Services.Inference;
using Hartonomous.Infrastructure.Services.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace Hartonomous.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering AI services (inference, generation, autonomous, content extraction)
/// </summary>
public static class AIServiceExtensions
{
    /// <summary>
    /// Registers all AI-related services (model ingestion, inference, generation, autonomous execution)
    /// </summary>
    public static IServiceCollection AddHartonomousAIServices(
        this IServiceCollection services)
    {
        // Domain services (Core layer business logic) - Scoped because they depend on DbContext
        services.AddScoped<IModelCapabilityService, ModelCapabilityService>();
        services.AddScoped<IInferenceMetadataService, InferenceMetadataService>();

        // Event enrichment
        services.AddScoped<IEventEnricher, EventEnricher>();

        // Atom ingestion (SQL CLR-based intelligent ingestion)
        services.AddScoped<IAtomIngestionService, SqlClrAtomIngestionService>();

        // Spatial inference and student model services
        services.AddScoped<ISpatialInferenceService, SpatialInferenceService>();
        services.AddScoped<IStudentModelService, StudentModelService>();

        // Model discovery and statistics
        services.AddScoped<IModelDiscoveryService, ModelDiscoveryService>();
        services.AddScoped<IIngestionStatisticsService, IngestionStatisticsService>();

        // Model format reading infrastructure
        services.AddScoped<Services.ModelFormats.GGUFParser>();
        services.AddScoped<Services.ModelFormats.GGUFDequantizer>();
        services.AddScoped<Services.ModelFormats.GGUFGeometryBuilder>();
        services.AddScoped<Services.ModelFormats.GGUFModelBuilder>();
        services.AddScoped<Services.ModelFormats.OnnxModelLoader>();
        services.AddScoped<Services.ModelFormats.TorchSharpModelLoader>();

        // Model format readers (vendor implementations)
        services.AddScoped<IModelFormatReader<GGUFMetadata>, Services.ModelFormats.Readers.GGUFModelReader>();
        services.AddScoped<IModelFormatReader<OnnxMetadata>, Services.ModelFormats.Readers.OnnxModelReader>();
        services.AddScoped<IModelFormatReader<PyTorchMetadata>, Services.ModelFormats.Readers.PyTorchModelReader>();
        services.AddScoped<IModelFormatReader<SafetensorsMetadata>, Services.ModelFormats.Readers.SafetensorsModelReader>();

        // Search services (semantic + spatial)
        services.AddScoped<ISemanticSearchService, Services.Search.SemanticSearchService>();
        services.AddScoped<ISpatialSearchService, Services.Search.SpatialSearchService>();

        // Feature extraction services
        services.AddScoped<ISemanticFeatureService, Services.Features.SemanticFeatureService>();

        // Inference services
        services.AddScoped<IEnsembleInferenceService, EnsembleInferenceService>();
        services.AddScoped<ITextGenerationService, TextGenerationService>();
        services.AddScoped<IInferenceService, InferenceOrchestrator>();
        services.AddScoped<IEmbeddingService, EmbeddingService>();

        // Time series prediction service
        // Note: TimeSeriesPredictionService requires connection string in constructor - register where needed

        // Multi-modal content generation suite (text, image, video, audio) - concrete class
        services.AddScoped<ContentGenerationSuite>();

        // Content ingestion orchestration - concrete class
        services.AddScoped<ContentIngestionService>();

        // Content extractors (registered as transient for per-request instances)
        services.AddTransient<IContentExtractor, TextContentExtractor>();
        services.AddTransient<IContentExtractor, TelemetryContentExtractor>();
        services.AddTransient<IContentExtractor, JsonApiContentExtractor>();
        services.AddTransient<IContentExtractor, HtmlContentExtractor>();
        services.AddTransient<IContentExtractor, DatabaseSyncExtractor>();
        services.AddTransient<IContentExtractor, DocumentContentExtractor>();
        services.AddTransient<IContentExtractor, VideoContentExtractor>();

        // Model ingestion processors and orchestrators - concrete classes
        services.AddScoped<ModelIngestionProcessor>();
        services.AddScoped<ModelIngestionOrchestrator>();
        services.AddScoped<ModelDownloader>();
        services.AddScoped<InferenceJobProcessor>();

        return services;
    }
}

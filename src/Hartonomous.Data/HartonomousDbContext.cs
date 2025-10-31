using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hartonomous.Data;

/// <summary>
/// Main DbContext for Hartonomous AI inference engine
/// Supports SQL Server 2025 VECTOR types, spatial data (via NetTopologySuite), and FILESTREAM
/// </summary>
public class HartonomousDbContext : DbContext
{
    public HartonomousDbContext(DbContextOptions<HartonomousDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        
        // Enable NetTopologySuite for spatial types (GEOMETRY/GEOGRAPHY)
        // Required for EF Core 10 to map LineString, Point, Polygon, etc. to SQL Server spatial types
        if (!optionsBuilder.IsConfigured)
        {
            // Configuration will be provided via DI, but ensure NTS is enabled
            optionsBuilder.UseSqlServer(x => x.UseNetTopologySuite());
        }
    }

    // Core model management
    public DbSet<Model> Models => Set<Model>();
    public DbSet<ModelLayer> ModelLayers => Set<ModelLayer>();
    public DbSet<CachedActivation> CachedActivations => Set<CachedActivation>();
    public DbSet<ModelMetadata> ModelMetadata => Set<ModelMetadata>();

    // Vocabulary (token-level embeddings stored via atom substrate views)
    public DbSet<TokenVocabulary> TokenVocabulary => Set<TokenVocabulary>();

    // Atomic storage (content-addressable deduplication)
    public DbSet<AtomicPixel> AtomicPixels => Set<AtomicPixel>();
    public DbSet<AtomicAudioSample> AtomicAudioSamples => Set<AtomicAudioSample>();
    public DbSet<AtomicTextToken> AtomicTextTokens => Set<AtomicTextToken>();

    // Multi-modal data
    public DbSet<Image> Images => Set<Image>();
    public DbSet<ImagePatch> ImagePatches => Set<ImagePatch>();
    public DbSet<AudioData> AudioData => Set<AudioData>();
    public DbSet<AudioFrame> AudioFrames => Set<AudioFrame>();
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<VideoFrame> VideoFrames => Set<VideoFrame>();
    public DbSet<TextDocument> TextDocuments => Set<TextDocument>();

    // Inference tracking
    public DbSet<InferenceRequest> InferenceRequests => Set<InferenceRequest>();
    public DbSet<InferenceStep> InferenceSteps => Set<InferenceStep>();

    // Atom substrate
    public DbSet<Atom> Atoms => Set<Atom>();
    public DbSet<AtomEmbedding> AtomEmbeddings => Set<AtomEmbedding>();
    public DbSet<AtomEmbeddingComponent> AtomEmbeddingComponents => Set<AtomEmbeddingComponent>();
    public DbSet<TensorAtom> TensorAtoms => Set<TensorAtom>();
    public DbSet<TensorAtomCoefficient> TensorAtomCoefficients => Set<TensorAtomCoefficient>();
    public DbSet<AtomRelation> AtomRelations => Set<AtomRelation>();
    public DbSet<IngestionJob> IngestionJobs => Set<IngestionJob>();
    public DbSet<IngestionJobAtom> IngestionJobAtoms => Set<IngestionJobAtom>();
    public DbSet<DeduplicationPolicy> DeduplicationPolicies => Set<DeduplicationPolicy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HartonomousDbContext).Assembly);

        // Global query filters and conventions
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Set default schema
            entityType.SetSchema("dbo");
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Configure DateTime to always use UTC and map to DATETIME2
        configurationBuilder.Properties<DateTime>()
            .HaveColumnType("datetime2");

        // Configure DateOnly to map to DATE
        configurationBuilder.Properties<DateOnly>()
            .HaveColumnType("date");
    }
}

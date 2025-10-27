using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hartonomous.Data;

/// <summary>
/// Main DbContext for Hartonomous AI inference engine
/// Supports SQL Server 2025 VECTOR types, spatial data, and FILESTREAM
/// </summary>
public class HartonomousDbContext : DbContext
{
    public HartonomousDbContext(DbContextOptions<HartonomousDbContext> options)
        : base(options)
    {
    }

    // Core model management
    public DbSet<Model> Models => Set<Model>();
    public DbSet<ModelLayer> ModelLayers => Set<ModelLayer>();
    public DbSet<CachedActivation> CachedActivations => Set<CachedActivation>();
    public DbSet<ModelMetadata> ModelMetadata => Set<ModelMetadata>();

    // Embeddings and vectors
    public DbSet<Embedding> Embeddings => Set<Embedding>();
    public DbSet<TokenVocabulary> TokenVocabulary => Set<TokenVocabulary>();

    // Inference tracking
    public DbSet<InferenceRequest> InferenceRequests => Set<InferenceRequest>();
    public DbSet<InferenceStep> InferenceSteps => Set<InferenceStep>();

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

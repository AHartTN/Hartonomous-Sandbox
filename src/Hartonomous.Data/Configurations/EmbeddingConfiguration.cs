using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hartonomous.Data.Configurations;

public class EmbeddingConfiguration : IEntityTypeConfiguration<Embedding>
{
    public void Configure(EntityTypeBuilder<Embedding> builder)
    {
        builder.ToTable("Embeddings_Production");

        builder.HasKey(e => e.EmbeddingId);

        builder.Property(e => e.SourceText)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.SourceType)
            .IsRequired()
            .HasMaxLength(50);

        // CRITICAL: SqlVector<float> to VECTOR(768) mapping
        // SqlVector is stored directly - EF Core + SqlClient 6.1.2 handle binary TDS transport
        builder.Property(e => e.EmbeddingFull)
            .HasColumnName("embedding_full")
            .HasColumnType("VECTOR(768)");
            // No value converter needed - SqlClient 6.1.2+ handles SqlVector natively

        builder.Property(e => e.EmbeddingModel)
            .HasMaxLength(100);

        builder.Property(e => e.Dimension)
            .HasDefaultValue(768);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(e => e.AccessCount)
            .HasDefaultValue(0);

        // Phase 2: ContentHash for deduplication
        builder.Property(e => e.ContentHash)
            .HasMaxLength(64); // SHA256 hex string
        
        builder.HasIndex(e => e.ContentHash)
            .HasDatabaseName("idx_content_hash");

        // TEMP: Commenting out computed spatial geometry column - causing EF Core type mapping issues
        // Will add this directly in migration or via raw SQL after table creation
        /*
        // Computed column for spatial geometry
        builder.Property<string>("SpatialGeometry")
            .HasColumnName("spatial_geometry")
            .HasColumnType("geometry")
            .HasComputedColumnSql(
                "geometry::STGeomFromText('POINT(' + " +
                "CAST([spatial_proj_x] AS NVARCHAR(50)) + ' ' + " +
                "CAST([spatial_proj_y] AS NVARCHAR(50)) + ')', 0)", 
                stored: true);

        // Spatial index (will be created via migration or raw SQL)
        // EF Core doesn't have full spatial index support, so we'll add this in migration
        builder.HasIndex("SpatialGeometry")
            .HasDatabaseName("idx_spatial_fine");
        */

        // NOTE: DiskANN VECTOR indexes commented out - they make tables read-only in RC1
        // Will be enabled in GA release
        // For now: O(n) scans with VECTOR_DISTANCE, acting as if indexes exist
        // Queries work correctly, just slower until GA
    }
}

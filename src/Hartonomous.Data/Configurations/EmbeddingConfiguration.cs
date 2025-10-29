using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;

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

        // Spatial geometry properties using NetTopologySuite
        builder.Property(e => e.SpatialGeometry)
            .HasColumnName("spatial_geometry")
            .HasColumnType("geometry");

        builder.Property(e => e.SpatialCoarse)
            .HasColumnName("spatial_coarse")
            .HasColumnType("geometry");

        // Spatial indexes for fast approximate search
        builder.HasIndex(e => e.SpatialGeometry)
            .HasDatabaseName("idx_spatial_fine");

        builder.HasIndex(e => e.SpatialCoarse)
            .HasDatabaseName("idx_spatial_coarse");
    }
}

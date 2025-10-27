using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public class ModelLayerConfiguration : IEntityTypeConfiguration<ModelLayer>
{
    public void Configure(EntityTypeBuilder<ModelLayer> builder)
    {
        builder.ToTable("ModelLayers");

        builder.HasKey(l => l.LayerId);

        builder.Property(l => l.LayerName)
            .HasMaxLength(100);

        builder.Property(l => l.LayerType)
            .HasMaxLength(50);

        // Layer weights as VECTOR (SQL Server 2025 native type)
        // Small layers: VECTOR(n) where n <= 1998 (float32) or 3996 (float16)
        // Large layers: chunk across multiple rows
        // VECTOR is already binary, efficient, queryable, and deduplicated - no compression needed
        builder.Property(l => l.Weights)
            .HasColumnType("VECTOR(1998)");  // Max dimension for float32

        builder.Property(l => l.QuantizationType)
            .HasMaxLength(20);

        // SQL Server 2025 native JSON type for layer parameters
        builder.Property(l => l.Parameters)
            .HasColumnType("JSON");

        builder.Property(l => l.CacheHitRate)
            .HasDefaultValue(0.0);

        // Indexes
        builder.HasIndex(l => new { l.ModelId, l.LayerIdx })
            .HasDatabaseName("idx_model_layer");

        builder.HasIndex(l => l.LayerType)
            .HasDatabaseName("idx_layer_type");

        // Relationships
        builder.HasMany(l => l.CachedActivations)
            .WithOne(ca => ca.Layer)
            .HasForeignKey(ca => ca.LayerId);
    }
}

using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Infrastructure.Data.Configurations;

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

        builder.Property(l => l.LayerAtomId);

        // Layer weights as GEOMETRY (LINESTRING ZM) for variable-dimension tensors
        // X = index, Y = weight, Z = importance/gradient, M = iteration/depth
        // No dimension limits (up to 1B+ points vs VECTOR's 1998 max)
        // Spatial indexes enable O(log n) queries for inference
        // EF Core maps NetTopologySuite.Geometries.LineString to SQL Server GEOMETRY
        builder.Property(l => l.WeightsGeometry)

            .HasColumnType("geometry");  // SQL Server GEOMETRY type

        builder.Property(l => l.PreviewPointCount)
            .HasColumnType("int");

        // Tensor shape as JSON array for reconstruction (e.g., "[3584, 3584]")
        builder.Property(l => l.TensorShape)
            .HasColumnType("NVARCHAR(200)");

        // Tensor data type (float32, float16, bfloat16)
        builder.Property(l => l.TensorDtype)
            .HasMaxLength(20)
            .HasDefaultValue("float32");

        builder.Property(l => l.QuantizationType)
            .HasMaxLength(20);

        // SQL Server 2025 native JSON type for layer parameters
        builder.Property(l => l.Parameters)
            .HasColumnType("JSON");

        builder.Property(l => l.CacheHitRate)
            .HasDefaultValue(0.0);

        builder.Property(l => l.ZMin)
            .HasColumnType("float");

        builder.Property(l => l.ZMax)
            .HasColumnType("float");

        builder.Property(l => l.MMin)
            .HasColumnType("float");

        builder.Property(l => l.MMax)
            .HasColumnType("float");

        builder.Property(l => l.MortonCode)
            .HasColumnType("bigint");

        // Indexes
        builder.HasIndex(l => new { l.ModelId, l.LayerIdx })
            .HasDatabaseName("IX_ModelLayers_ModelId_LayerIdx");

        builder.HasIndex(l => l.LayerType)
            .HasDatabaseName("IX_ModelLayers_LayerType");

        builder.HasIndex(l => new { l.ModelId, l.ZMin, l.ZMax })
            .HasDatabaseName("IX_ModelLayers_Z_Range");

        builder.HasIndex(l => new { l.ModelId, l.MMin, l.MMax })
            .HasDatabaseName("IX_ModelLayers_M_Range");

        builder.HasIndex(l => l.MortonCode)
            .HasDatabaseName("IX_ModelLayers_Morton");

        // Relationships
        builder.HasMany(l => l.CachedActivations)
            .WithOne(ca => ca.Layer)
            .HasForeignKey(ca => ca.LayerId);

        builder.HasMany(l => l.TensorAtoms)
            .WithOne(t => t.Layer)
            .HasForeignKey(t => t.LayerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(l => l.TensorAtomCoefficients)
            .WithOne(c => c.ParentLayer)
            .HasForeignKey(c => c.ParentLayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.LayerAtom)
            .WithMany()
            .HasForeignKey(l => l.LayerAtomId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(l => l.LayerAtomId)
            .HasDatabaseName("IX_ModelLayers_LayerAtomId");

        builder.HasMany(l => l.TensorSegments)
            .WithOne(s => s.Layer)
            .HasForeignKey(s => s.LayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

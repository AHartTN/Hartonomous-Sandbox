using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class ModelLayerConfiguration : IEntityTypeConfiguration<ModelLayer>
{
    public void Configure(EntityTypeBuilder<ModelLayer> builder)
    {
        builder.ToTable("ModelLayer", "dbo");
        builder.HasKey(e => new { e.LayerId });

        builder.Property(e => e.LayerId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.AvgComputeTimeMs)
            .HasColumnType("float")
            ;

        builder.Property(e => e.CacheHitRate)
            .HasColumnType("float")
            ;

        builder.Property(e => e.LayerAtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.LayerIdx)
            .HasColumnType("int")
            ;

        builder.Property(e => e.LayerName)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            ;

        builder.Property(e => e.LayerType)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            ;

        builder.Property(e => e.Mmax)
            .HasColumnName("MMax")
            .HasColumnType("float")
            ;

        builder.Property(e => e.Mmin)
            .HasColumnName("MMin")
            .HasColumnType("float")
            ;

        builder.Property(e => e.ModelId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.MortonCode)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.ParameterCount)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.Parameters)
            .HasColumnType("json")
            ;

        builder.Property(e => e.PreviewPointCount)
            .HasColumnType("int")
            ;

        builder.Property(e => e.QuantizationScale)
            .HasColumnType("float")
            ;

        builder.Property(e => e.QuantizationType)
            .HasColumnType("nvarchar(20)")
            .HasMaxLength(20)
            ;

        builder.Property(e => e.QuantizationZeroPoint)
            .HasColumnType("float")
            ;

        builder.Property(e => e.TensorDtype)
            .HasColumnType("nvarchar(20)")
            .HasMaxLength(20)
            ;

        builder.Property(e => e.TensorShape)
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            ;

        builder.Property(e => e.WeightsGeometry)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.Zmax)
            .HasColumnName("ZMax")
            .HasColumnType("float")
            ;

        builder.Property(e => e.Zmin)
            .HasColumnName("ZMin")
            .HasColumnType("float")
            ;

        builder.HasOne(d => d.LayerAtom)
            .WithMany(p => p.ModelLayer)
            .HasForeignKey(d => new { d.LayerAtomId })
            .OnDelete(DeleteBehavior.ClientSetNull)
            ;

        builder.HasOne(d => d.Model)
            .WithMany(p => p.ModelLayer)
            .HasForeignKey(d => new { d.ModelId })
            ;
    }
}

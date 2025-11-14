using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class LayerTensorSegmentConfiguration : IEntityTypeConfiguration<LayerTensorSegment>
{
    public void Configure(EntityTypeBuilder<LayerTensorSegment> builder)
    {
        builder.ToTable("LayerTensorSegments", "dbo");
        builder.HasKey(e => new { e.LayerTensorSegmentId });

        builder.Property(e => e.LayerTensorSegmentId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.GeometryFootprint)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.LayerId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.Mmax)
            .HasColumnName("MMax")
            .HasColumnType("float")
            ;

        builder.Property(e => e.Mmin)
            .HasColumnName("MMin")
            .HasColumnType("float")
            ;

        builder.Property(e => e.MortonCode)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.PayloadRowGuid)
            .HasColumnType("uniqueidentifier")
            ;

        builder.Property(e => e.PointCount)
            .HasColumnType("int")
            ;

        builder.Property(e => e.PointOffset)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.QuantizationScale)
            .HasColumnType("float")
            ;

        builder.Property(e => e.QuantizationType)
            .HasColumnType("nvarchar(20)")
            .HasMaxLength(20)
            .IsRequired()
            ;

        builder.Property(e => e.QuantizationZeroPoint)
            .HasColumnType("float")
            ;

        builder.Property(e => e.RawPayload)
            .HasColumnType("varbinary(max)")
            .IsRequired()
            ;

        builder.Property(e => e.SegmentOrdinal)
            .HasColumnType("int")
            ;

        builder.Property(e => e.Zmax)
            .HasColumnName("ZMax")
            .HasColumnType("float")
            ;

        builder.Property(e => e.Zmin)
            .HasColumnName("ZMin")
            .HasColumnType("float")
            ;

        builder.HasOne(d => d.Layer)
            .WithMany(p => p.LayerTensorSegments)
            .HasForeignKey(d => new { d.LayerId })
            ;

        builder.HasIndex(e => new { e.LayerId, e.Mmin, e.Mmax })
            .HasDatabaseName("IX_LayerTensorSegments_M_Range")
            ;

        builder.HasIndex(e => new { e.MortonCode })
            .HasDatabaseName("IX_LayerTensorSegments_Morton")
            ;

        builder.HasIndex(e => new { e.LayerId, e.Zmin, e.Zmax })
            .HasDatabaseName("IX_LayerTensorSegments_Z_Range")
            ;

        builder.HasIndex(e => new { e.LayerId, e.SegmentOrdinal })
            .HasDatabaseName("UX_LayerTensorSegments_LayerId_SegmentOrdinal")
            .IsUnique()
            ;

        builder.HasIndex(e => new { e.PayloadRowGuid })
            .HasDatabaseName("UX_LayerTensorSegments_PayloadRowGuid")
            .IsUnique()
            ;
    }
}

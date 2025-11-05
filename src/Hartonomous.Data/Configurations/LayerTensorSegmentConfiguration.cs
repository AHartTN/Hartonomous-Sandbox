using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public class LayerTensorSegmentConfiguration : IEntityTypeConfiguration<LayerTensorSegment>
{
    public void Configure(EntityTypeBuilder<LayerTensorSegment> builder)
    {
        builder.ToTable("LayerTensorSegments");

        builder.HasKey(s => s.LayerTensorSegmentId);

        builder.Property(s => s.SegmentOrdinal)
            .HasColumnType("int");

        builder.Property(s => s.PointOffset)
            .HasColumnType("bigint");

        builder.Property(s => s.PointCount)
            .HasColumnType("int");

        builder.Property(s => s.QuantizationType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.QuantizationScale)
            .HasColumnType("float");

        builder.Property(s => s.QuantizationZeroPoint)
            .HasColumnType("float");

        builder.Property(s => s.ZMin)
            .HasColumnType("float");

        builder.Property(s => s.ZMax)
            .HasColumnType("float");

        builder.Property(s => s.MMin)
            .HasColumnType("float");

        builder.Property(s => s.MMax)
            .HasColumnType("float");

        builder.Property(s => s.MortonCode)
            .HasColumnType("bigint");

        builder.Property(s => s.GeometryFootprint)
            .HasColumnType("geometry");

        builder.Property(s => s.RawPayload)
            .HasColumnType("VARBINARY(MAX) FILESTREAM")
            .IsRequired();

        builder.Property(s => s.PayloadRowGuid)
            .HasColumnType("uniqueidentifier ROWGUIDCOL")
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(s => s.CreatedAt)
            .HasColumnType("DATETIME2")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(s => new { s.LayerId, s.SegmentOrdinal })
            .IsUnique()
            .HasDatabaseName("UX_LayerTensorSegments_LayerId_SegmentOrdinal");

        builder.HasIndex(s => new { s.LayerId, s.ZMin, s.ZMax })
            .HasDatabaseName("IX_LayerTensorSegments_Z_Range");

        builder.HasIndex(s => new { s.LayerId, s.MMin, s.MMax })
            .HasDatabaseName("IX_LayerTensorSegments_M_Range");

        builder.HasIndex(s => s.MortonCode)
            .HasDatabaseName("IX_LayerTensorSegments_Morton");
    }
}
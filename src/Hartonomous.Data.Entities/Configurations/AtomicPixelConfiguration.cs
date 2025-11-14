using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AtomicPixelConfiguration : IEntityTypeConfiguration<AtomicPixel>
{
    public void Configure(EntityTypeBuilder<AtomicPixel> builder)
    {
        builder.ToTable("AtomicPixels", "dbo");
        builder.HasKey(e => new { e.PixelHash });

        builder.Property(e => e.PixelHash)
            .HasColumnType("binary(32)")
            .HasMaxLength(32)
            .IsRequired()
            ;

        builder.Property(e => e.A)
            .HasColumnType("tinyint")
            ;

        builder.Property(e => e.B)
            .HasColumnType("tinyint")
            ;

        builder.Property(e => e.ColorPoint)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.FirstSeen)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.G)
            .HasColumnType("tinyint")
            ;

        builder.Property(e => e.LastReferenced)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.R)
            .HasColumnType("tinyint")
            ;

        builder.Property(e => e.ReferenceCount)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.RgbaBytes)
            .HasColumnType("varbinary(4)")
            .HasMaxLength(4)
            .IsRequired()
            ;

        builder.HasIndex(e => new { e.R, e.G, e.B })
            .HasDatabaseName("IX_AtomicPixels_RGB")
            ;

        builder.HasIndex(e => new { e.ReferenceCount })
            .HasDatabaseName("IX_AtomicPixels_References")
            ;

        builder.HasIndex(e => new { e.ColorPoint })
            .HasDatabaseName("SIDX_AtomicPixels_ColorSpace")
            ;
    }
}

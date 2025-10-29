using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Configurations;

public class AtomicPixelConfiguration : IEntityTypeConfiguration<AtomicPixel>
{
    public void Configure(EntityTypeBuilder<AtomicPixel> builder)
    {
        builder.HasKey(p => p.PixelHash);

        builder.Property(p => p.PixelHash)
            .HasColumnType("BINARY(32)")
            .IsRequired();

        builder.Property(p => p.R)
            .HasColumnType("TINYINT")
            .IsRequired();

        builder.Property(p => p.G)
            .HasColumnType("TINYINT")
            .IsRequired();

        builder.Property(p => p.B)
            .HasColumnType("TINYINT")
            .IsRequired();

        builder.Property(p => p.A)
            .HasColumnType("TINYINT")
            .HasDefaultValue((byte)255)
            .IsRequired();

        // NTS Point for spatial color representation
        builder.Property(p => p.ColorPoint)
            .HasColumnType("GEOMETRY");

        builder.Property(p => p.ReferenceCount)
            .HasColumnType("BIGINT")
            .HasDefaultValue(0L);

        builder.Property(p => p.FirstSeen)
            .HasColumnType("DATETIME2")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(p => p.LastReferenced)
            .HasColumnType("DATETIME2")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(p => p.ColorPoint)
            .HasDatabaseName("idx_color_space");
    }
}
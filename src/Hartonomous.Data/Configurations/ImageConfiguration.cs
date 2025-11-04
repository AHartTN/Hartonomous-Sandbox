using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public sealed class ImageConfiguration : IEntityTypeConfiguration<Image>
{
    public void Configure(EntityTypeBuilder<Image> builder)
    {
        builder.ToTable("Images", "dbo");

        builder.HasKey(e => e.ImageId);
        builder.Property(e => e.ImageId).ValueGeneratedOnAdd();

        builder.Property(e => e.SourcePath).HasMaxLength(500);
        builder.Property(e => e.SourceUrl).HasMaxLength(1000);
        builder.Property(e => e.Width).IsRequired();
        builder.Property(e => e.Height).IsRequired();
        builder.Property(e => e.Channels).IsRequired();
        builder.Property(e => e.Format).HasMaxLength(20);

        // Spatial representations
        builder.Property(e => e.PixelCloud).HasColumnType("GEOMETRY");
        builder.Property(e => e.EdgeMap).HasColumnType("GEOMETRY");
        builder.Property(e => e.ObjectRegions).HasColumnType("GEOMETRY");
        builder.Property(e => e.SaliencyRegions).HasColumnType("GEOMETRY");

        // Vector representations
        builder.Property(e => e.GlobalEmbedding).HasColumnType("VECTOR(1536)");
        builder.Property(e => e.GlobalEmbeddingDim);

        // Metadata
        builder.Property(e => e.Metadata).HasColumnType("JSON");

        builder.Property(e => e.IngestionDate).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.LastAccessed);
        builder.Property(e => e.AccessCount).HasDefaultValue(0L);

        // Indexes
        builder.HasIndex(e => new { e.Width, e.Height })
            .HasDatabaseName("IX_Images_Width_Height");

        builder.HasIndex(e => e.IngestionDate)
            .HasDatabaseName("IX_Images_IngestionDate")
            .IsDescending();

        // Relationships
        builder.HasMany(e => e.Patches)
            .WithOne(p => p.Image)
            .HasForeignKey(p => p.ImageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

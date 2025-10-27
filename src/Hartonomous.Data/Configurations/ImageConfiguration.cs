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
        builder.Property(e => e.ImageId).HasColumnName("image_id").ValueGeneratedOnAdd();

        builder.Property(e => e.SourcePath).HasColumnName("source_path").HasMaxLength(500);
        builder.Property(e => e.SourceUrl).HasColumnName("source_url").HasMaxLength(1000);

        // Raw data
        builder.Property(e => e.RawData).HasColumnName("raw_data");
        builder.Property(e => e.Width).HasColumnName("width").IsRequired();
        builder.Property(e => e.Height).HasColumnName("height").IsRequired();
        builder.Property(e => e.Channels).HasColumnName("channels").IsRequired();
        builder.Property(e => e.Format).HasColumnName("format").HasMaxLength(20);

        // Spatial representations
        builder.Property(e => e.PixelCloud).HasColumnName("pixel_cloud").HasColumnType("GEOMETRY");
        builder.Property(e => e.EdgeMap).HasColumnName("edge_map").HasColumnType("GEOMETRY");
        builder.Property(e => e.ObjectRegions).HasColumnName("object_regions").HasColumnType("GEOMETRY");
        builder.Property(e => e.SaliencyRegions).HasColumnName("saliency_regions").HasColumnType("GEOMETRY");

        // Vector representations
        builder.Property(e => e.GlobalEmbedding).HasColumnName("global_embedding").HasColumnType("VECTOR(1536)");
        builder.Property(e => e.GlobalEmbeddingDim).HasColumnName("global_embedding_dim");

        // Metadata
        builder.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("JSON");

        builder.Property(e => e.IngestionDate).HasColumnName("ingestion_date").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.LastAccessed).HasColumnName("last_accessed");
        builder.Property(e => e.AccessCount).HasColumnName("access_count").HasDefaultValue(0L);

        // Indexes
        builder.HasIndex(e => new { e.Width, e.Height }).HasDatabaseName("idx_dimensions");
        builder.HasIndex(e => e.IngestionDate).HasDatabaseName("idx_ingestion").IsDescending();

        // Relationships
        builder.HasMany(e => e.Patches)
            .WithOne(p => p.Image)
            .HasForeignKey(p => p.ImageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

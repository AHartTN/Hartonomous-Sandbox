using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public sealed class VideoConfiguration : IEntityTypeConfiguration<Video>
{
    public void Configure(EntityTypeBuilder<Video> builder)
    {
        builder.ToTable("Videos", "dbo");

        builder.HasKey(e => e.VideoId);
        builder.Property(e => e.VideoId).HasColumnName("video_id").ValueGeneratedOnAdd();

        builder.Property(e => e.SourcePath).HasColumnName("source_path").HasMaxLength(500);

        // Raw data
        builder.Property(e => e.RawData).HasColumnName("raw_data");
        builder.Property(e => e.Fps).HasColumnName("fps").IsRequired();
        builder.Property(e => e.DurationMs).HasColumnName("duration_ms").IsRequired();
        builder.Property(e => e.ResolutionWidth).HasColumnName("resolution_width").IsRequired();
        builder.Property(e => e.ResolutionHeight).HasColumnName("resolution_height").IsRequired();
        builder.Property(e => e.NumFrames).HasColumnName("num_frames").IsRequired();
        builder.Property(e => e.Format).HasColumnName("format").HasMaxLength(20);

        // Global representation
        builder.Property(e => e.GlobalEmbedding).HasColumnName("global_embedding").HasColumnType("VECTOR(768)");
        builder.Property(e => e.GlobalEmbeddingDim).HasColumnName("global_embedding_dim");

        // Metadata
        builder.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("JSON");

        builder.Property(e => e.IngestionDate).HasColumnName("ingestion_date").HasDefaultValueSql("SYSUTCDATETIME()");

        // Indexes
        builder.HasIndex(e => new { e.ResolutionWidth, e.ResolutionHeight }).HasDatabaseName("idx_resolution");
        builder.HasIndex(e => e.IngestionDate).HasDatabaseName("idx_ingestion").IsDescending();

        // Relationships
        builder.HasMany(e => e.Frames)
            .WithOne(f => f.Video)
            .HasForeignKey(f => f.VideoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

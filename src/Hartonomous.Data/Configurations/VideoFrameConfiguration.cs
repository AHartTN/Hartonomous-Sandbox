using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public sealed class VideoFrameConfiguration : IEntityTypeConfiguration<VideoFrame>
{
    public void Configure(EntityTypeBuilder<VideoFrame> builder)
    {
        builder.ToTable("VideoFrames", "dbo");

        builder.HasKey(e => e.FrameId);
        builder.Property(e => e.FrameId).HasColumnName("frame_id").ValueGeneratedOnAdd();

        builder.Property(e => e.VideoId).HasColumnName("video_id").IsRequired();
        builder.Property(e => e.FrameNumber).HasColumnName("frame_number").IsRequired();
        builder.Property(e => e.TimestampMs).HasColumnName("timestamp_ms").IsRequired();

        // Frame as spatial data
        builder.Property(e => e.PixelCloud).HasColumnName("pixel_cloud").HasColumnType("GEOMETRY");
        builder.Property(e => e.ObjectRegions).HasColumnName("object_regions").HasColumnType("GEOMETRY");

        // Motion information
        builder.Property(e => e.MotionVectors).HasColumnName("motion_vectors").HasColumnType("GEOMETRY");
        builder.Property(e => e.OpticalFlow).HasColumnName("optical_flow").HasColumnType("GEOMETRY");

        // Frame embedding
        builder.Property(e => e.FrameEmbedding).HasColumnName("frame_embedding").HasColumnType("VECTOR(768)");

        // Deduplication
        builder.Property(e => e.PerceptualHash).HasColumnName("perceptual_hash").HasMaxLength(8);

        // Indexes
        builder.HasIndex(e => new { e.VideoId, e.FrameNumber }).HasDatabaseName("idx_video_frame").IsUnique();
        builder.HasIndex(e => new { e.VideoId, e.TimestampMs }).HasDatabaseName("idx_timestamp");
    }
}

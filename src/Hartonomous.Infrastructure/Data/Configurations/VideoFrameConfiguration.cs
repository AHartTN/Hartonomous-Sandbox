using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Infrastructure.Data.Configurations;

public sealed class VideoFrameConfiguration : IEntityTypeConfiguration<VideoFrame>
{
    public void Configure(EntityTypeBuilder<VideoFrame> builder)
    {
        builder.ToTable("VideoFrames", "dbo");

        builder.HasKey(e => e.FrameId);
        builder.Property(e => e.FrameId).ValueGeneratedOnAdd();

        builder.Property(e => e.VideoId).IsRequired();
        builder.Property(e => e.FrameNumber).IsRequired();
        builder.Property(e => e.TimestampMs).IsRequired();

        // Frame as spatial data
        builder.Property(e => e.PixelCloud).HasColumnType("GEOMETRY");
        builder.Property(e => e.ObjectRegions).HasColumnType("GEOMETRY");

        // Motion information
        builder.Property(e => e.MotionVectors).HasColumnType("GEOMETRY");
        builder.Property(e => e.OpticalFlow).HasColumnType("GEOMETRY");

        // Frame embedding
        builder.Property(e => e.FrameEmbedding).HasColumnType("VECTOR(768)");

        // Deduplication
        builder.Property(e => e.PerceptualHash).HasMaxLength(8);

        // Indexes
        builder.HasIndex(e => new { e.VideoId, e.FrameNumber })
            .HasDatabaseName("IX_VideoFrames_VideoId_FrameNumber")
            .IsUnique();

        builder.HasIndex(e => new { e.VideoId, e.TimestampMs })
            .HasDatabaseName("IX_VideoFrames_VideoId_TimestampMs");
    }
}

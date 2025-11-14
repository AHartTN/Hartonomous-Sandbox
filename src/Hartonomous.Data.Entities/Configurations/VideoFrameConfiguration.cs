using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class VideoFrameConfiguration : IEntityTypeConfiguration<VideoFrame>
{
    public void Configure(EntityTypeBuilder<VideoFrame> builder)
    {
        builder.ToTable("VideoFrames", "dbo");
        builder.HasKey(e => new { e.FrameId });

        builder.Property(e => e.FrameId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.FrameEmbedding)
            .HasColumnType("vector(1998)")
            .HasMaxLength(1998)
            ;

        builder.Property(e => e.FrameNumber)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.MotionVectors)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.ObjectRegions)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.OpticalFlow)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.PerceptualHash)
            .HasColumnType("varbinary(8)")
            .HasMaxLength(8)
            ;

        builder.Property(e => e.PixelCloud)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.TimestampMs)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.VideoId)
            .HasColumnType("bigint")
            ;

        builder.HasOne(d => d.Video)
            .WithMany(p => p.VideoFrames)
            .HasForeignKey(d => new { d.VideoId })
            ;

        builder.HasIndex(e => new { e.MotionVectors })
            .HasDatabaseName("IX_VideoFrames_MotionVectors")
            ;

        builder.HasIndex(e => new { e.VideoId, e.FrameNumber })
            .HasDatabaseName("IX_VideoFrames_VideoId_FrameNumber")
            .IsUnique()
            ;

        builder.HasIndex(e => new { e.VideoId, e.TimestampMs })
            .HasDatabaseName("IX_VideoFrames_VideoId_TimestampMs")
            ;
    }
}

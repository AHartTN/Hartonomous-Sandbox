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
        builder.Property(e => e.VideoId).ValueGeneratedOnAdd();

        builder.Property(e => e.SourcePath).HasMaxLength(500);

        // Raw data
        builder.Property(e => e.RawData);
        builder.Property(e => e.Fps).IsRequired();
        builder.Property(e => e.DurationMs).IsRequired();
        builder.Property(e => e.ResolutionWidth).IsRequired();
        builder.Property(e => e.ResolutionHeight).IsRequired();
        builder.Property(e => e.NumFrames).IsRequired();
        builder.Property(e => e.Format).HasMaxLength(20);

        // Global representation
        builder.Property(e => e.GlobalEmbedding).HasColumnType("VECTOR(768)");
        builder.Property(e => e.GlobalEmbeddingDim);

        // Metadata
        builder.Property(e => e.Metadata).HasColumnType("JSON");

        builder.Property(e => e.IngestionDate).HasDefaultValueSql("SYSUTCDATETIME()");

        // Indexes
        builder.HasIndex(e => new { e.ResolutionWidth, e.ResolutionHeight })
            .HasDatabaseName("IX_Videos_ResolutionWidth_ResolutionHeight");

        builder.HasIndex(e => e.IngestionDate)
            .HasDatabaseName("IX_Videos_IngestionDate")
            .IsDescending();

        // Relationships
        builder.HasMany(e => e.Frames)
            .WithOne(f => f.Video)
            .HasForeignKey(f => f.VideoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

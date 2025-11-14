using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class VideoConfiguration : IEntityTypeConfiguration<Video>
{
    public void Configure(EntityTypeBuilder<Video> builder)
    {
        builder.ToTable("Videos", "dbo");
        builder.HasKey(e => new { e.VideoId });

        builder.Property(e => e.VideoId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.DurationMs)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.Format)
            .HasColumnType("nvarchar(20)")
            .HasMaxLength(20)
            ;

        builder.Property(e => e.Fps)
            .HasColumnType("int")
            ;

        builder.Property(e => e.GlobalEmbedding)
            .HasColumnType("vector(1998)")
            .HasMaxLength(1998)
            ;

        builder.Property(e => e.IngestionDate)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Metadata)
            .HasColumnType("json")
            ;

        builder.Property(e => e.NumFrames)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.ResolutionHeight)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ResolutionWidth)
            .HasColumnType("int")
            ;

        builder.Property(e => e.SourcePath)
            .HasColumnType("nvarchar(500)")
            .HasMaxLength(500)
            ;

        builder.HasIndex(e => new { e.IngestionDate })
            .HasDatabaseName("IX_Videos_IngestionDate")
            ;

        builder.HasIndex(e => new { e.ResolutionWidth, e.ResolutionHeight })
            .HasDatabaseName("IX_Videos_ResolutionWidth_ResolutionHeight")
            ;
    }
}

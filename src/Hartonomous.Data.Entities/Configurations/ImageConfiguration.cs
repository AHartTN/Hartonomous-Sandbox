using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class ImageConfiguration : IEntityTypeConfiguration<Image>
{
    public void Configure(EntityTypeBuilder<Image> builder)
    {
        builder.ToTable("Images", "dbo");
        builder.HasKey(e => new { e.ImageId });

        builder.Property(e => e.ImageId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.AccessCount)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.Channels)
            .HasColumnType("int")
            ;

        builder.Property(e => e.EdgeMap)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.Format)
            .HasColumnType("nvarchar(20)")
            .HasMaxLength(20)
            ;

        builder.Property(e => e.GlobalEmbedding)
            .HasColumnType("vector(1998)")
            .HasMaxLength(1998)
            ;

        builder.Property(e => e.Height)
            .HasColumnType("int")
            ;

        builder.Property(e => e.IngestionDate)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.LastAccessed)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Metadata)
            .HasColumnType("json")
            ;

        builder.Property(e => e.ObjectRegions)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.PixelCloud)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.SaliencyRegions)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.SourcePath)
            .HasColumnType("nvarchar(500)")
            .HasMaxLength(500)
            ;

        builder.Property(e => e.SourceUrl)
            .HasColumnType("nvarchar(1000)")
            .HasMaxLength(1000)
            ;

        builder.Property(e => e.Width)
            .HasColumnType("int")
            ;

        builder.HasIndex(e => new { e.IngestionDate })
            .HasDatabaseName("IX_Images_IngestionDate")
            ;

        builder.HasIndex(e => new { e.ObjectRegions })
            .HasDatabaseName("IX_Images_ObjectRegions")
            ;

        builder.HasIndex(e => new { e.Width, e.Height })
            .HasDatabaseName("IX_Images_Width_Height")
            ;
    }
}

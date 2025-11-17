using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class GenerationStreamSegmentsConfiguration : IEntityTypeConfiguration<GenerationStreamSegments>
{
    public void Configure(EntityTypeBuilder<GenerationStreamSegments> builder)
    {
        builder.ToTable("GenerationStreamSegments", "dbo");
        builder.HasKey(e => new { e.SegmentId });

        builder.Property(e => e.SegmentId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.ContentType)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.EmbeddingVector)
            .HasColumnType("vector(1998)")
            .HasMaxLength(1998)
            ;

        builder.Property(e => e.GenerationStreamId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.Metadata)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.PayloadData)
            .HasColumnType("varbinary(max)")
            ;

        builder.Property(e => e.SegmentKind)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.Property(e => e.SegmentOrdinal)
            .HasColumnType("int")
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            ;

        builder.HasOne(d => d.GenerationStream)
            .WithMany(p => p.GenerationStreamSegments)
            .HasForeignKey(d => new { d.GenerationStreamId })
            ;

        builder.HasIndex(e => new { e.CreatedAt })
            .HasDatabaseName("IX_GenerationStreamSegments_CreatedAt")
            ;

        builder.HasIndex(e => new { e.GenerationStreamId, e.SegmentOrdinal })
            .HasDatabaseName("IX_GenerationStreamSegments_GenerationStreamId")
            ;

        builder.HasIndex(e => new { e.SegmentKind })
            .HasDatabaseName("IX_GenerationStreamSegments_SegmentKind")
            ;
    }
}

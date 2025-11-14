using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class GenerationStreamConfiguration : IEntityTypeConfiguration<GenerationStream>
{
    public void Configure(EntityTypeBuilder<GenerationStream> builder)
    {
        builder.ToTable("GenerationStreams", "provenance");
        builder.HasKey(e => new { e.StreamId });

        builder.Property(e => e.StreamId)
            .HasColumnType("uniqueidentifier")
            ;

        builder.Property(e => e.ContextMetadata)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.CreatedUtc)
            .HasColumnType("datetime2(3)")
            ;

        builder.Property(e => e.GeneratedAtomIds)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.GenerationStreamId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.Model)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            ;

        builder.Property(e => e.ModelId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ProvenanceStream)
            .HasColumnType("varbinary(max)")
            ;

        builder.Property(e => e.Scope)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            ;

        builder.HasOne(d => d.ModelNavigation)
            .WithMany(p => p.GenerationStreams)
            .HasForeignKey(d => new { d.ModelId })
            .OnDelete(DeleteBehavior.ClientSetNull)
            ;

        builder.HasIndex(e => new { e.CreatedUtc })
            .HasDatabaseName("IX_GenerationStreams_CreatedUtc")
            ;

        builder.HasIndex(e => new { e.GenerationStreamId })
            .HasDatabaseName("IX_GenerationStreams_GenerationStreamId")
            ;

        builder.HasIndex(e => new { e.Model })
            .HasDatabaseName("IX_GenerationStreams_Model")
            ;

        builder.HasIndex(e => new { e.ModelId })
            .HasDatabaseName("IX_GenerationStreams_ModelId")
            ;

        builder.HasIndex(e => new { e.Scope })
            .HasDatabaseName("IX_GenerationStreams_Scope")
            ;

        builder.HasIndex(e => new { e.TenantId })
            .HasDatabaseName("IX_GenerationStreams_TenantId")
            ;

        builder.HasIndex(e => new { e.GenerationStreamId })
            .HasDatabaseName("UQ_GenerationStreams_GenerationStreamId")
            .IsUnique()
            ;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AtomGraphNodeConfiguration : IEntityTypeConfiguration<AtomGraphNode>
{
    public void Configure(EntityTypeBuilder<AtomGraphNode> builder)
    {
        builder.ToTable("AtomGraphNodes", "graph");
        builder.HasKey(e => new { e.AtomId });

        builder.Property(e => e.AtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.CanonicalText)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.GraphId1ed5587659f24875930e33ed4194a3a6)
            .HasColumnName("graph_id_1ED5587659F24875930E33ED4194A3A6")
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.Metadata)
            .HasColumnType("json")
            ;

        builder.Property(e => e.Modality)
            .HasColumnType("nvarchar(64)")
            .HasMaxLength(64)
            .IsRequired()
            ;

        builder.Property(e => e.NodeIdA4600067acf04785ae52bb0b40c7d43b)
            .HasColumnName("$node_id_A4600067ACF04785AE52BB0B40C7D43B")
            .HasColumnType("nvarchar(1000)")
            .HasMaxLength(1000)
            .IsRequired()
            ;

        builder.Property(e => e.PayloadLocator)
            .HasColumnType("nvarchar(512)")
            .HasMaxLength(512)
            ;

        builder.Property(e => e.Semantics)
            .HasColumnType("json")
            ;

        builder.Property(e => e.SourceType)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            ;

        builder.Property(e => e.SourceUri)
            .HasColumnType("nvarchar(2048)")
            .HasMaxLength(2048)
            ;

        builder.Property(e => e.SpatialKey)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.Subtype)
            .HasColumnType("nvarchar(64)")
            .HasMaxLength(64)
            ;

        builder.Property(e => e.UpdatedAt)
            .HasColumnType("datetime2")
            ;

        builder.HasIndex(e => new { e.AtomId })
            .HasDatabaseName("IX_AtomGraphNodes_AtomId")
            ;

        builder.HasIndex(e => new { e.CreatedAt })
            .HasDatabaseName("IX_AtomGraphNodes_CreatedAt")
            ;

        builder.HasIndex(e => new { e.Modality, e.Subtype })
            .HasDatabaseName("IX_AtomGraphNodes_Modality")
            ;

        builder.HasIndex(e => new { e.Modality, e.Subtype })
            .HasDatabaseName("IX_AtomGraphNodes_Modality_Subtype")
            ;

        builder.HasIndex(e => new { e.SpatialKey })
            .HasDatabaseName("SIX_AtomGraphNodes_SpatialKey")
            ;

        builder.HasIndex(e => new { e.AtomId })
            .HasDatabaseName("UX_AtomGraphNodes_AtomId")
            .IsUnique()
            ;
    }
}

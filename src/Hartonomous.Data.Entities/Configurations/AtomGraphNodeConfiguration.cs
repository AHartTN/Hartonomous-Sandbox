using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Entities.Configurations;

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

        builder.Property(e => e.GraphId494f8ff335f24c699e7cd43e9f927f38)
            .HasColumnName("graph_id_494F8FF335F24C699E7CD43E9F927F38")
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

        builder.Property(e => e.NodeId4da534f68b2342b9b1d83cbdaea1bdab)
            .HasColumnName("$node_id_4DA534F68B2342B9B1D83CBDAEA1BDAB")
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
            .HasDatabaseName("UX_AtomGraphNodes_AtomId")
            .IsUnique()
            ;
    }
}

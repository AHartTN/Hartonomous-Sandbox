using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AtomGraphNodesConfiguration : IEntityTypeConfiguration<AtomGraphNodes>
{
    public void Configure(EntityTypeBuilder<AtomGraphNodes> builder)
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

        builder.Property(e => e.GraphId8316578acbaa4d43b0aea45baf11ee8a)
            .HasColumnName("graph_id_8316578ACBAA4D43B0AEA45BAF11EE8A")
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

        builder.Property(e => e.NodeIdE82207a9821c447c97a4a1ba75b3025d)
            .HasColumnName("$node_id_E82207A9821C447C97A4A1BA75B3025D")
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

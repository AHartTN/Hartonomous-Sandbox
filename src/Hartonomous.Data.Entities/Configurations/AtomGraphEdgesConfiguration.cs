using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AtomGraphEdgesConfiguration : IEntityTypeConfiguration<AtomGraphEdges>
{
    public void Configure(EntityTypeBuilder<AtomGraphEdges> builder)
    {
        builder.ToTable("AtomGraphEdges", "graph");
        builder.HasKey(e => new { e.AtomRelationId });

        builder.Property(e => e.AtomRelationId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.EdgeId6854515447584ef793d8b88940f42811)
            .HasColumnName("$edge_id_6854515447584EF793D8B88940F42811")
            .HasColumnType("nvarchar(1000)")
            .HasMaxLength(1000)
            .IsRequired()
            ;

        builder.Property(e => e.FromId33a86cbf5fef41cb8bd2367f9f7fb292)
            .HasColumnName("from_id_33A86CBF5FEF41CB8BD2367F9F7FB292")
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.FromId8832880a53e242528f26affcb9f6dde9)
            .HasColumnName("$from_id_8832880A53E242528F26AFFCB9F6DDE9")
            .HasColumnType("nvarchar(1000)")
            .HasMaxLength(1000)
            ;

        builder.Property(e => e.FromObjId44e30bdc381441b480860e5c26c9cbe6)
            .HasColumnName("from_obj_id_44E30BDC381441B480860E5C26C9CBE6")
            .HasColumnType("int")
            ;

        builder.Property(e => e.GraphId54ce94e6f75d41f68f1e2b740e8ed972)
            .HasColumnName("graph_id_54CE94E6F75D41F68F1E2B740E8ED972")
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.Metadata)
            .HasColumnType("json")
            ;

        builder.Property(e => e.RelationType)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            .IsRequired()
            ;

        builder.Property(e => e.SpatialExpression)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.ToId7d4d4ca90296499ab2a761f4032ef0c7)
            .HasColumnName("to_id_7D4D4CA90296499AB2A761F4032EF0C7")
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.ToId848cfeedb29947e18ecbc48b9a9fed21)
            .HasColumnName("$to_id_848CFEEDB29947E18ECBC48B9A9FED21")
            .HasColumnType("nvarchar(1000)")
            .HasMaxLength(1000)
            ;

        builder.Property(e => e.ToObjId25d973ef142042ba981b6cd8bbad1beb)
            .HasColumnName("to_obj_id_25D973EF142042BA981B6CD8BBAD1BEB")
            .HasColumnType("int")
            ;

        builder.Property(e => e.UpdatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Weight)
            .HasColumnType("float")
            ;

        builder.HasIndex(e => new { e.AtomRelationId })
            .HasDatabaseName("UX_AtomGraphEdges_AtomRelationId")
            .IsUnique()
            ;
    }
}

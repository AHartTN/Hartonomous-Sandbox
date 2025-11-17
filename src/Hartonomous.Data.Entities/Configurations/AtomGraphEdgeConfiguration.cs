using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Entities.Configurations;

public class AtomGraphEdgeConfiguration : IEntityTypeConfiguration<AtomGraphEdge>
{
    public void Configure(EntityTypeBuilder<AtomGraphEdge> builder)
    {
        builder.ToTable("AtomGraphEdges", "graph");
        builder.HasKey(e => new { e.AtomRelationId });

        builder.Property(e => e.AtomRelationId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.EdgeId47be36ae1c4b4f3bb7f0e08849fa977e)
            .HasColumnName("$edge_id_47BE36AE1C4B4F3BB7F0E08849FA977E")
            .HasColumnType("nvarchar(1000)")
            .HasMaxLength(1000)
            .IsRequired()
            ;

        builder.Property(e => e.FromIdA3fdce16d30d4b9691ba136908f40759)
            .HasColumnName("$from_id_A3FDCE16D30D4B9691BA136908F40759")
            .HasColumnType("nvarchar(1000)")
            .HasMaxLength(1000)
            ;

        builder.Property(e => e.FromIdFc1219066bdc4776b44a5a0412bcf924)
            .HasColumnName("from_id_FC1219066BDC4776B44A5A0412BCF924")
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.FromObjId071043cc800b4943aeffc058465ef7fa)
            .HasColumnName("from_obj_id_071043CC800B4943AEFFC058465EF7FA")
            .HasColumnType("int")
            ;

        builder.Property(e => e.GraphId429ab56d0ebd458fb38c43c3b8267534)
            .HasColumnName("graph_id_429AB56D0EBD458FB38C43C3B8267534")
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

        builder.Property(e => e.ToId579809caf8c941d88f251a1c6ee5fd07)
            .HasColumnName("$to_id_579809CAF8C941D88F251A1C6EE5FD07")
            .HasColumnType("nvarchar(1000)")
            .HasMaxLength(1000)
            ;

        builder.Property(e => e.ToIdF2762fc34e0f412c8fc5f6ed00f26692)
            .HasColumnName("to_id_F2762FC34E0F412C8FC5F6ED00F26692")
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.ToObjId80f993575fbd4a9ea686d1cab73ad763)
            .HasColumnName("to_obj_id_80F993575FBD4A9EA686D1CAB73AD763")
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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

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

        builder.Property(e => e.EdgeIdEe9be59b11634b148dba809bd1d99150)
            .HasColumnName("$edge_id_EE9BE59B11634B148DBA809BD1D99150")
            .HasColumnType("nvarchar(1000)")
            .HasMaxLength(1000)
            .IsRequired()
            ;

        builder.Property(e => e.FromId1e22d35020c54c1da39133f04210fc27)
            .HasColumnName("from_id_1E22D35020C54C1DA39133F04210FC27")
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.FromId607e9fcfb54c4a409ab7bdc29b63f086)
            .HasColumnName("$from_id_607E9FCFB54C4A409AB7BDC29B63F086")
            .HasColumnType("nvarchar(1000)")
            .HasMaxLength(1000)
            ;

        builder.Property(e => e.FromObjIdDb1d84b22b074350a65c32aba4c92a17)
            .HasColumnName("from_obj_id_DB1D84B22B074350A65C32ABA4C92A17")
            .HasColumnType("int")
            ;

        builder.Property(e => e.GraphIdC0374105abf94d8690a28e3930c45799)
            .HasColumnName("graph_id_C0374105ABF94D8690A28E3930C45799")
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

        builder.Property(e => e.ToIdD48121dd004c41b0b88470dfc6226c34)
            .HasColumnName("$to_id_D48121DD004C41B0B88470DFC6226C34")
            .HasColumnType("nvarchar(1000)")
            .HasMaxLength(1000)
            ;

        builder.Property(e => e.ToIdFfbd0b6425204fd2a03bd072f908138d)
            .HasColumnName("to_id_FFBD0B6425204FD2A03BD072F908138D")
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.ToObjId4f26395fa31342c986b9312a47cc5f26)
            .HasColumnName("to_obj_id_4F26395FA31342C986B9312A47CC5F26")
            .HasColumnType("int")
            ;

        builder.Property(e => e.UpdatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Weight)
            .HasColumnType("float")
            ;

        builder.HasIndex(e => new { e.CreatedAt })
            .HasDatabaseName("IX_AtomGraphEdges_CreatedAt")
            ;

        builder.HasIndex(e => new { e.RelationType })
            .HasDatabaseName("IX_AtomGraphEdges_RelationType")
            ;

        builder.HasIndex(e => new { e.RelationType })
            .HasDatabaseName("IX_AtomGraphEdges_Type")
            ;

        builder.HasIndex(e => new { e.Weight })
            .HasDatabaseName("IX_AtomGraphEdges_Weight")
            ;

        builder.HasIndex(e => new { e.SpatialExpression })
            .HasDatabaseName("SIX_AtomGraphEdges_SpatialExpression")
            ;

        builder.HasIndex(e => new { e.AtomRelationId })
            .HasDatabaseName("UX_AtomGraphEdges_AtomRelationId")
            .IsUnique()
            ;
    }
}

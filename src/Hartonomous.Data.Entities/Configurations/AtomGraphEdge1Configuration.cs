using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Entities.Configurations;

public class AtomGraphEdge1Configuration : IEntityTypeConfiguration<AtomGraphEdge1>
{
    public void Configure(EntityTypeBuilder<AtomGraphEdge1> builder)
    {
        builder.ToTable("AtomGraphEdges", "provenance");
        builder.HasKey(e => new { e.EdgeId });

        builder.Property(e => e.EdgeId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.DependencyType)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            ;

        builder.Property(e => e.EdgeType)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            ;

        builder.Property(e => e.FromAtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.Metadata)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ToAtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.Weight)
            .HasColumnType("float")
            ;

        builder.HasIndex(e => new { e.DependencyType })
            .HasDatabaseName("IX_AtomGraphEdges_DependencyType")
            ;

        builder.HasIndex(e => new { e.FromAtomId })
            .HasDatabaseName("IX_AtomGraphEdges_FromId")
            ;

        builder.HasIndex(e => new { e.ToAtomId })
            .HasDatabaseName("IX_AtomGraphEdges_ToId")
            ;
    }
}

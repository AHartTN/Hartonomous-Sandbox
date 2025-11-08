using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Infrastructure.Data.Configurations;

public class AtomGraphNodeConfiguration : IEntityTypeConfiguration<AtomGraphNode>
{
    public void Configure(EntityTypeBuilder<AtomGraphNode> builder)
    {
        builder.ToTable("AtomGraphNodes", "graph");

        builder.HasKey(e => e.NodeId);

        builder.Property(e => e.NodeId)
            .UseIdentityColumn();

        builder.Property(e => e.CreatedUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // Indexes
        builder.HasIndex(e => e.AtomId)
            .HasDatabaseName("IX_AtomGraphNodes_AtomId");

        builder.HasIndex(e => e.NodeType)
            .HasDatabaseName("IX_AtomGraphNodes_NodeType");

        builder.HasIndex(e => e.CreatedUtc)
            .HasDatabaseName("IX_AtomGraphNodes_CreatedUtc");

        // Foreign key to Atom
        builder.HasOne(e => e.Atom)
            .WithMany()
            .HasForeignKey(e => e.AtomId)
            .OnDelete(DeleteBehavior.Cascade);

        // Note: SQL Graph relationships are configured at the database level
        // The OutgoingEdges and IncomingEdges are handled by SQL Graph engine
        builder.Ignore(e => e.OutgoingEdges);
        builder.Ignore(e => e.IncomingEdges);
    }
}

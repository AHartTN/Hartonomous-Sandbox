using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Infrastructure.Data.Configurations;

public class AtomGraphEdgeConfiguration : IEntityTypeConfiguration<AtomGraphEdge>
{
    public void Configure(EntityTypeBuilder<AtomGraphEdge> builder)
    {
        builder.ToTable("AtomGraphEdges", "graph");

        builder.HasKey(e => e.EdgeId);

        builder.Property(e => e.EdgeId)
            .UseIdentityColumn();

        builder.Property(e => e.Weight)
            .HasDefaultValue(1.0);

        builder.Property(e => e.CreatedUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // Indexes
        builder.HasIndex(e => e.EdgeType)
            .HasDatabaseName("IX_AtomGraphEdges_EdgeType");

        builder.HasIndex(e => e.Weight)
            .HasDatabaseName("IX_AtomGraphEdges_Weight");

        builder.HasIndex(e => e.CreatedUtc)
            .HasDatabaseName("IX_AtomGraphEdges_CreatedUtc");

        // Check constraints
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_AtomGraphEdges_EdgeType",
                "[EdgeType] IN ('DerivedFrom', 'ComponentOf', 'SimilarTo', 'Uses', 'InputTo', 'OutputFrom', 'BindsToConcept')");
            t.HasCheckConstraint("CK_AtomGraphEdges_Weight",
                "[Weight] >= 0.0 AND [Weight] <= 1.0");
        });

        // Note: SQL Graph FROM/TO relationships are configured at the database level
        // The FromNode and ToNode navigation properties are handled by SQL Graph engine
        // For EF Core, we ignore these navigation properties as they're managed by SQL Graph
        builder.Ignore(e => e.FromNode);
        builder.Ignore(e => e.ToNode);
    }
}

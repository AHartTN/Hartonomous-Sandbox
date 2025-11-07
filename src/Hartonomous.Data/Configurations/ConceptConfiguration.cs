using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public class ConceptConfiguration : IEntityTypeConfiguration<Concept>
{
    public void Configure(EntityTypeBuilder<Concept> builder)
    {
        builder.ToTable("Concepts", "provenance");

        builder.HasKey(e => e.ConceptId);

        builder.Property(e => e.ConceptId)
            .UseIdentityColumn();

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.MemberCount)
            .HasDefaultValue(0);

        builder.Property(e => e.DiscoveredAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // Indexes
        builder.HasIndex(e => e.ConceptName)
            .HasDatabaseName("IX_Concepts_ConceptName");

        builder.HasIndex(e => new { e.ModelId, e.IsActive })
            .HasDatabaseName("IX_Concepts_ModelId_IsActive");

        builder.HasIndex(e => e.DiscoveryMethod)
            .HasDatabaseName("IX_Concepts_DiscoveryMethod");

        builder.HasIndex(e => e.CoherenceScore)
            .HasDatabaseName("IX_Concepts_CoherenceScore")
            .IsDescending();

        // Foreign key
        builder.HasOne(e => e.Model)
            .WithMany()
            .HasForeignKey(e => e.ModelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AtomConceptConfiguration : IEntityTypeConfiguration<AtomConcept>
{
    public void Configure(EntityTypeBuilder<AtomConcept> builder)
    {
        builder.ToTable("AtomConcepts", "provenance");
        builder.HasKey(e => new { e.AtomConceptId });

        builder.Property(e => e.AtomConceptId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.AssignedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.AtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.ConceptId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.DistanceToCentroid)
            .HasColumnType("float")
            ;

        builder.Property(e => e.IsPrimary)
            .HasColumnType("bit")
            ;

        builder.Property(e => e.MembershipScore)
            .HasColumnType("float")
            ;

        builder.Property(e => e.Similarity)
            .HasColumnType("float")
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            ;

        builder.HasOne(d => d.Atom)
            .WithMany(p => p.AtomConcepts)
            .HasForeignKey(d => new { d.AtomId })
            ;

        builder.HasOne(d => d.Concept)
            .WithMany(p => p.AtomConcepts)
            .HasForeignKey(d => new { d.ConceptId })
            ;

        builder.HasIndex(e => new { e.ConceptId })
            .HasDatabaseName("IX_AtomConcepts_ConceptId")
            ;

        builder.HasIndex(e => new { e.AtomId, e.ConceptId })
            .HasDatabaseName("UX_AtomConcepts_AtomId_ConceptId")
            .IsUnique()
            ;
    }
}

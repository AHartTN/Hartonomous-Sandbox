using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class ConceptEvolutionConfiguration : IEntityTypeConfiguration<ConceptEvolution>
{
    public void Configure(EntityTypeBuilder<ConceptEvolution> builder)
    {
        builder.ToTable("ConceptEvolution", "provenance");
        builder.HasKey(e => new { e.EvolutionId });

        builder.Property(e => e.EvolutionId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.AtomCountDelta)
            .HasColumnType("int")
            ;

        builder.Property(e => e.CentroidShift)
            .HasColumnType("float")
            ;

        builder.Property(e => e.CoherenceChange)
            .HasColumnType("float")
            ;

        builder.Property(e => e.CoherenceDelta)
            .HasColumnType("float")
            ;

        builder.Property(e => e.ConceptId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.EvolutionReason)
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            ;

        builder.Property(e => e.EvolutionType)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            ;

        builder.Property(e => e.MemberCountChange)
            .HasColumnType("int")
            ;

        builder.Property(e => e.NewCentroid)
            .HasColumnType("varbinary(max)")
            .IsRequired()
            ;

        builder.Property(e => e.PreviousCentroid)
            .HasColumnType("varbinary(max)")
            .IsRequired()
            ;

        builder.Property(e => e.RecordedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            ;

        builder.HasOne(d => d.Concept)
            .WithMany(p => p.ConceptEvolution)
            .HasForeignKey(d => new { d.ConceptId })
            ;

        builder.HasIndex(e => new { e.ConceptId, e.RecordedAt })
            .HasDatabaseName("IX_ConceptEvolution_ConceptId_RecordedAt")
            ;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class ConceptConfiguration : IEntityTypeConfiguration<Concept>
{
    public void Configure(EntityTypeBuilder<Concept> builder)
    {
        builder.ToTable("Concepts", "provenance");
        builder.HasKey(e => new { e.ConceptId });

        builder.Property(e => e.ConceptId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.AtomCount)
            .HasColumnType("int")
            ;

        builder.Property(e => e.Centroid)
            .HasColumnType("varbinary(max)")
            ;

        builder.Property(e => e.CentroidVector)
            .HasColumnType("varbinary(max)")
            .IsRequired()
            ;

        builder.Property(e => e.Coherence)
            .HasColumnType("float")
            ;

        builder.Property(e => e.CoherenceScore)
            .HasColumnType("float")
            ;

        builder.Property(e => e.ConceptName)
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired()
            ;

        builder.Property(e => e.Description)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.DiscoveredAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.DiscoveryMethod)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired()
            ;

        builder.Property(e => e.IsActive)
            .HasColumnType("bit")
            ;

        builder.Property(e => e.LastUpdatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.MemberCount)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ModelId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.SeparationScore)
            .HasColumnType("float")
            ;

        builder.Property(e => e.SpatialBucket)
            .HasColumnType("int")
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.VectorDimension)
            .HasColumnType("int")
            ;

        builder.HasOne(d => d.Model)
            .WithMany(p => p.Concepts)
            .HasForeignKey(d => new { d.ModelId })
            ;

        builder.HasIndex(e => new { e.CoherenceScore })
            .HasDatabaseName("IX_Concepts_CoherenceScore")
            ;

        builder.HasIndex(e => new { e.ConceptName })
            .HasDatabaseName("IX_Concepts_ConceptName")
            ;

        builder.HasIndex(e => new { e.DiscoveryMethod })
            .HasDatabaseName("IX_Concepts_DiscoveryMethod")
            ;

        builder.HasIndex(e => new { e.ModelId, e.IsActive })
            .HasDatabaseName("IX_Concepts_ModelId_IsActive")
            ;
    }
}

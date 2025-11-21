using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Entities.Configurations;

public class ConceptConfiguration : IEntityTypeConfiguration<Concept>
{
    public void Configure(EntityTypeBuilder<Concept> builder)
    {
        builder.ToTable("Concept", "dbo");
        builder.HasKey(e => new { e.ConceptId });

        builder.Property(e => e.ConceptId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.AtomCount)
            .HasColumnType("int")
            ;

        builder.Property(e => e.CentroidVector)
            .HasColumnType("varbinary(max)")
            ;

        builder.Property(e => e.ConceptName)
            .HasColumnType("nvarchar(256)")
            .HasMaxLength(256)
            .IsRequired()
            ;

        builder.Property(e => e.ConceptType)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            ;

        builder.Property(e => e.Confidence)
            .HasColumnType("decimal(5,4)")
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.CreatedBy)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            ;

        builder.Property(e => e.Description)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.Domain)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.Metadata)
            .HasColumnType("json")
            ;

        builder.Property(e => e.ParentConceptId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.Radius)
            .HasColumnType("float")
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.UpdatedAt)
            .HasColumnType("datetime2")
            ;

        builder.HasOne(d => d.ParentConcept)
            .WithMany(p => p.InverseParentConcept)
            .HasForeignKey(d => new { d.ParentConceptId })
            .OnDelete(DeleteBehavior.ClientSetNull)
            ;

        builder.HasIndex(e => new { e.ConceptName })
            .HasDatabaseName("IX_Concept_Name")
            ;

        builder.HasIndex(e => new { e.ParentConceptId })
            .HasDatabaseName("IX_Concept_Parent")
            ;

        builder.HasIndex(e => new { e.TenantId, e.ConceptType })
            .HasDatabaseName("IX_Concept_TenantId")
            ;

        builder.HasIndex(e => new { e.Domain })
            .HasDatabaseName("SIDX_Concept_Domain")
            ;

        builder.HasIndex(e => new { e.TenantId, e.ConceptName })
            .HasDatabaseName("UQ_Concept_TenantName")
            .IsUnique()
            ;
    }
}

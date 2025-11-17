using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Entities.Configurations;

public class ProvenanceValidationResultConfiguration : IEntityTypeConfiguration<ProvenanceValidationResult>
{
    public void Configure(EntityTypeBuilder<ProvenanceValidationResult> builder)
    {
        builder.ToTable("ProvenanceValidationResults", "dbo");
        builder.HasKey(e => new { e.Id });

        builder.Property(e => e.Id)
            .HasColumnType("int")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.OperationId)
            .HasColumnType("uniqueidentifier")
            ;

        builder.Property(e => e.OverallStatus)
            .HasColumnType("nvarchar(20)")
            .HasMaxLength(20)
            .IsRequired()
            ;

        builder.Property(e => e.ValidatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.ValidationDurationMs)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ValidationResults)
            .HasColumnType("json")
            ;

        builder.HasOne(d => d.Operation)
            .WithMany(p => p.ProvenanceValidationResults)
            .HasForeignKey(d => new { d.OperationId })
            ;

        builder.HasIndex(e => new { e.OperationId })
            .HasDatabaseName("IX_ProvenanceValidationResults_OperationId")
            ;

        builder.HasIndex(e => new { e.OverallStatus })
            .HasDatabaseName("IX_ProvenanceValidationResults_Status")
            ;

        builder.HasIndex(e => new { e.ValidatedAt })
            .HasDatabaseName("IX_ProvenanceValidationResults_ValidatedAt")
            ;
    }
}

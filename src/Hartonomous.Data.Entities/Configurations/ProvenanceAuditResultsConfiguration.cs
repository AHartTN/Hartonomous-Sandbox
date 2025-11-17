using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class ProvenanceAuditResultsConfiguration : IEntityTypeConfiguration<ProvenanceAuditResults>
{
    public void Configure(EntityTypeBuilder<ProvenanceAuditResults> builder)
    {
        builder.ToTable("ProvenanceAuditResults", "dbo");
        builder.HasKey(e => new { e.Id });

        builder.Property(e => e.Id)
            .HasColumnType("int")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.Anomalies)
            .HasColumnType("json")
            ;

        builder.Property(e => e.AuditDurationMs)
            .HasColumnType("int")
            ;

        builder.Property(e => e.AuditPeriodEnd)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.AuditPeriodStart)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.AuditedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.AverageSegmentCount)
            .HasColumnType("float")
            ;

        builder.Property(e => e.AverageValidationScore)
            .HasColumnType("float")
            ;

        builder.Property(e => e.FailedOperations)
            .HasColumnType("int")
            ;

        builder.Property(e => e.Scope)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            ;

        builder.Property(e => e.TotalOperations)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ValidOperations)
            .HasColumnType("int")
            ;

        builder.Property(e => e.WarningOperations)
            .HasColumnType("int")
            ;

        builder.HasIndex(e => new { e.AuditPeriodStart, e.AuditPeriodEnd })
            .HasDatabaseName("IX_ProvenanceAuditResults_AuditPeriod")
            ;

        builder.HasIndex(e => new { e.AuditedAt })
            .HasDatabaseName("IX_ProvenanceAuditResults_AuditedAt")
            ;
    }
}

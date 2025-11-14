using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class BillingQuotaViolationConfiguration : IEntityTypeConfiguration<BillingQuotaViolation>
{
    public void Configure(EntityTypeBuilder<BillingQuotaViolation> builder)
    {
        builder.ToTable("BillingQuotaViolations", "dbo");
        builder.HasKey(e => new { e.ViolationId });

        builder.Property(e => e.ViolationId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.CurrentUsage)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.Notes)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.QuotaLimit)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.Resolved)
            .HasColumnType("bit")
            ;

        builder.Property(e => e.ResolvedUtc)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.UsageType)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.Property(e => e.ViolatedUtc)
            .HasColumnType("datetime2")
            ;

        builder.HasIndex(e => new { e.TenantId, e.ViolatedUtc })
            .HasDatabaseName("IX_BillingQuotaViolations_Tenant")
            ;

        builder.HasIndex(e => new { e.Resolved })
            .HasDatabaseName("IX_BillingQuotaViolations_Unresolved")
            ;
    }
}

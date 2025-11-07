using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public class BillingUsageLedgerConfiguration : IEntityTypeConfiguration<BillingUsageLedger>
{
    public void Configure(EntityTypeBuilder<BillingUsageLedger> builder)
    {
        builder.ToTable("BillingUsageLedger");

        builder.HasKey(e => e.LedgerId);

        builder.Property(e => e.LedgerId)
            .UseIdentityColumn();

        builder.Property(e => e.Units)
            .HasPrecision(18, 6);

        builder.Property(e => e.BaseRate)
            .HasPrecision(18, 6);

        builder.Property(e => e.Multiplier)
            .HasPrecision(18, 6)
            .HasDefaultValue(1.0m);

        builder.Property(e => e.TotalCost)
            .HasPrecision(18, 6);

        builder.Property(e => e.TimestampUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // Indexes for tenant queries (most common access pattern)
        builder.HasIndex(e => new { e.TenantId, e.TimestampUtc })
            .HasDatabaseName("IX_BillingUsageLedger_TenantId_Timestamp")
            .IncludeProperties(e => new { e.Operation, e.TotalCost });

        // Index for operation analytics
        builder.HasIndex(e => new { e.Operation, e.TimestampUtc })
            .HasDatabaseName("IX_BillingUsageLedger_Operation_Timestamp")
            .IncludeProperties(e => new { e.TenantId, e.Units, e.TotalCost });
    }
}
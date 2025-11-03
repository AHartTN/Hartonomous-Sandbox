using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public sealed class BillingRatePlanConfiguration : IEntityTypeConfiguration<BillingRatePlan>
{
    public void Configure(EntityTypeBuilder<BillingRatePlan> builder)
    {
        builder.ToTable("BillingRatePlans");

        builder.HasKey(plan => plan.RatePlanId);

        builder.Property(plan => plan.RatePlanId)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(plan => plan.TenantId)
            .HasMaxLength(64);

        builder.Property(plan => plan.PlanCode)
            .IsRequired()
            .HasMaxLength(64)
            .HasDefaultValue(string.Empty);

        builder.Property(plan => plan.Name)
            .IsRequired()
            .HasMaxLength(128)
            .HasDefaultValue(string.Empty);

        builder.Property(plan => plan.Description)
            .HasMaxLength(256);

        builder.Property(plan => plan.DefaultRate)
            .HasColumnType("decimal(18,6)")
            .HasDefaultValue(0.01m);

        builder.Property(plan => plan.MonthlyFee)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(plan => plan.UnitPricePerDcu)
            .HasColumnType("decimal(18,6)")
            .HasDefaultValue(0.00008m);

        builder.Property(plan => plan.IncludedPublicStorageGb)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(plan => plan.IncludedPrivateStorageGb)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(plan => plan.IncludedSeatCount)
            .HasDefaultValue(1);

        builder.Property(plan => plan.AllowsPrivateData)
            .HasDefaultValue(false);

        builder.Property(plan => plan.CanQueryPublicCorpus)
            .HasDefaultValue(false);

        builder.Property(plan => plan.IsActive)
            .HasDefaultValue(true);

        builder.Property(plan => plan.CreatedUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(plan => plan.UpdatedUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(plan => new { plan.TenantId, plan.IsActive })
            .HasDatabaseName("IX_BillingRatePlans_Tenant_IsActive")
            .IncludeProperties(plan => plan.UpdatedUtc);

        builder.HasIndex(plan => new { plan.TenantId, plan.PlanCode })
            .IsUnique()
            .HasFilter("[PlanCode] <> ''")
            .HasDatabaseName("UX_BillingRatePlans_Tenant_PlanCode");
    }
}

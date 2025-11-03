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

        builder.Property(plan => plan.Name)
            .IsRequired()
            .HasMaxLength(128)
            .HasDefaultValue(string.Empty);

        builder.Property(plan => plan.Description)
            .HasMaxLength(256);

        builder.Property(plan => plan.DefaultRate)
            .HasColumnType("decimal(18,6)")
            .HasDefaultValue(0.01m);

        builder.Property(plan => plan.IsActive)
            .HasDefaultValue(true);

        builder.Property(plan => plan.CreatedUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(plan => plan.UpdatedUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(plan => new { plan.TenantId, plan.IsActive })
            .HasDatabaseName("IX_BillingRatePlans_Tenant_IsActive")
            .IncludeProperties(plan => plan.UpdatedUtc);
    }
}

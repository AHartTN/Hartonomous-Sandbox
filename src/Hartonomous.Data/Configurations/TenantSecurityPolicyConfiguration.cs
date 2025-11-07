using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public class TenantSecurityPolicyConfiguration : IEntityTypeConfiguration<TenantSecurityPolicy>
{
    public void Configure(EntityTypeBuilder<TenantSecurityPolicy> builder)
    {
        builder.ToTable("TenantSecurityPolicy");

        builder.HasKey(e => e.PolicyId);

        builder.Property(e => e.PolicyId)
            .UseIdentityColumn();

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // Indexes
        builder.HasIndex(e => new { e.TenantId, e.PolicyType })
            .HasDatabaseName("IX_TenantSecurityPolicy_TenantId_PolicyType");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_TenantSecurityPolicy_IsActive");

        builder.HasIndex(e => new { e.EffectiveFrom, e.EffectiveTo })
            .HasDatabaseName("IX_TenantSecurityPolicy_EffectiveDates");
    }
}
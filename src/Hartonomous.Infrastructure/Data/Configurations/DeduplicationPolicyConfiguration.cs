using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Infrastructure.Data.Configurations;

public class DeduplicationPolicyConfiguration : IEntityTypeConfiguration<DeduplicationPolicy>
{
    public void Configure(EntityTypeBuilder<DeduplicationPolicy> builder)
    {
        builder.ToTable("DeduplicationPolicies");

        builder.HasKey(p => p.DeduplicationPolicyId);

        builder.Property(p => p.PolicyName)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(p => p.PolicyName)
            .IsUnique()
            .HasDatabaseName("UX_DeduplicationPolicies_PolicyName");

        builder.Property(p => p.SemanticThreshold)
            .HasColumnType("float");

        builder.Property(p => p.SpatialThreshold)
            .HasColumnType("float");

        builder.Property(p => p.Metadata)
            .HasColumnType("JSON");

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }
}

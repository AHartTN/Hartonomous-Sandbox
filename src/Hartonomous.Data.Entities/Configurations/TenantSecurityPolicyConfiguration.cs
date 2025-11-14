using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class TenantSecurityPolicyConfiguration : IEntityTypeConfiguration<TenantSecurityPolicy>
{
    public void Configure(EntityTypeBuilder<TenantSecurityPolicy> builder)
    {
        builder.ToTable("TenantSecurityPolicy", "dbo");
        builder.HasKey(e => new { e.PolicyId });

        builder.Property(e => e.PolicyId)
            .HasColumnType("int")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.CreatedBy)
            .HasColumnType("nvarchar(256)")
            .HasMaxLength(256)
            ;

        builder.Property(e => e.CreatedUtc)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.EffectiveFrom)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.EffectiveTo)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.IsActive)
            .HasColumnType("bit")
            ;

        builder.Property(e => e.PolicyName)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired()
            ;

        builder.Property(e => e.PolicyRules)
            .HasColumnType("nvarchar(max)")
            .IsRequired()
            ;

        builder.Property(e => e.PolicyType)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            .IsRequired()
            ;

        builder.Property(e => e.UpdatedBy)
            .HasColumnType("nvarchar(256)")
            .HasMaxLength(256)
            ;

        builder.Property(e => e.UpdatedUtc)
            .HasColumnType("datetime2")
            ;

        builder.HasIndex(e => new { e.EffectiveFrom, e.EffectiveTo })
            .HasDatabaseName("IX_TenantSecurityPolicy_EffectiveDates")
            ;

        builder.HasIndex(e => new { e.IsActive })
            .HasDatabaseName("IX_TenantSecurityPolicy_IsActive")
            ;

        builder.HasIndex(e => new { e.TenantId, e.PolicyType })
            .HasDatabaseName("IX_TenantSecurityPolicy_TenantId_PolicyType")
            ;
    }
}

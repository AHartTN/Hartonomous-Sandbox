using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class BillingTenantQuotaConfiguration : IEntityTypeConfiguration<BillingTenantQuota>
{
    public void Configure(EntityTypeBuilder<BillingTenantQuota> builder)
    {
        builder.ToTable("BillingTenantQuotas", "dbo");
        builder.HasKey(e => new { e.QuotaId });

        builder.Property(e => e.QuotaId)
            .HasColumnType("int")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.CreatedUtc)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Description)
            .HasColumnType("nvarchar(500)")
            .HasMaxLength(500)
            ;

        builder.Property(e => e.IsActive)
            .HasColumnType("bit")
            ;

        builder.Property(e => e.MetadataJson)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.QuotaLimit)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.ResetInterval)
            .HasColumnType("nvarchar(20)")
            .HasMaxLength(20)
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.UpdatedUtc)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.UsageType)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.HasIndex(e => new { e.TenantId, e.UsageType, e.IsActive })
            .HasDatabaseName("IX_BillingTenantQuotas_Tenant")
            ;
    }
}

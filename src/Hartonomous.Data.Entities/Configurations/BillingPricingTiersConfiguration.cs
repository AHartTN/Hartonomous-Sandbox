using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class BillingPricingTiersConfiguration : IEntityTypeConfiguration<BillingPricingTiers>
{
    public void Configure(EntityTypeBuilder<BillingPricingTiers> builder)
    {
        builder.ToTable("BillingPricingTiers", "dbo");
        builder.HasKey(e => new { e.TierId });

        builder.Property(e => e.TierId)
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

        builder.Property(e => e.EffectiveFrom)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.EffectiveTo)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.MetadataJson)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.UnitPrice)
            .HasColumnType("decimal(18,8)")
            ;

        builder.Property(e => e.UnitType)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.Property(e => e.UsageType)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.HasIndex(e => new { e.UsageType, e.UnitType, e.EffectiveFrom })
            .HasDatabaseName("IX_BillingPricingTiers_UsageType")
            ;
    }
}

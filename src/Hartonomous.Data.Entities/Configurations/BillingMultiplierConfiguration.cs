using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class BillingMultiplierConfiguration : IEntityTypeConfiguration<BillingMultiplier>
{
    public void Configure(EntityTypeBuilder<BillingMultiplier> builder)
    {
        builder.ToTable("BillingMultiplier", "dbo");
        builder.HasKey(e => new { e.MultiplierId });

        builder.Property(e => e.MultiplierId)
            .HasColumnType("uniqueidentifier")
            ;

        builder.Property(e => e.CreatedUtc)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Dimension)
            .HasColumnType("nvarchar(32)")
            .HasMaxLength(32)
            .IsRequired()
            ;

        builder.Property(e => e.IsActive)
            .HasColumnType("bit")
            ;

        builder.Property(e => e.Key)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            .IsRequired()
            ;

        builder.Property(e => e.Multiplier)
            .HasColumnType("decimal(18,6)")
            ;

        builder.Property(e => e.RatePlanId)
            .HasColumnType("uniqueidentifier")
            ;

        builder.Property(e => e.UpdatedUtc)
            .HasColumnType("datetime2")
            ;

        builder.HasOne(d => d.RatePlan)
            .WithMany(p => p.BillingMultiplier)
            .HasForeignKey(d => new { d.RatePlanId })
            ;
    }
}

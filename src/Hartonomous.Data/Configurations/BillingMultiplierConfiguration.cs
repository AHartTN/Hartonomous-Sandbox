using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public sealed class BillingMultiplierConfiguration : IEntityTypeConfiguration<BillingMultiplier>
{
    public void Configure(EntityTypeBuilder<BillingMultiplier> builder)
    {
        builder.ToTable("BillingMultipliers");

        builder.HasKey(multiplier => multiplier.MultiplierId);

        builder.Property(multiplier => multiplier.MultiplierId)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(multiplier => multiplier.RatePlanId)
            .IsRequired();

        builder.Property(multiplier => multiplier.Dimension)
            .IsRequired()
            .HasMaxLength(32)
            .HasDefaultValue(string.Empty);

        builder.Property(multiplier => multiplier.Key)
            .IsRequired()
            .HasMaxLength(128)
            .HasDefaultValue(string.Empty);

        builder.Property(multiplier => multiplier.Multiplier)
            .HasColumnType("decimal(18,6)");

        builder.Property(multiplier => multiplier.IsActive)
            .HasDefaultValue(true);

        builder.Property(multiplier => multiplier.CreatedUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(multiplier => multiplier.UpdatedUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(multiplier => multiplier.RatePlan)
            .WithMany(plan => plan.Multipliers)
            .HasForeignKey(multiplier => multiplier.RatePlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(multiplier => new { multiplier.RatePlanId, multiplier.Dimension, multiplier.Key })
            .IsUnique()
            .HasDatabaseName("UX_BillingMultipliers_Active")
            .HasFilter("[IsActive] = 1");
    }
}

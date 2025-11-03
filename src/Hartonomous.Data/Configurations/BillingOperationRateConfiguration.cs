using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public sealed class BillingOperationRateConfiguration : IEntityTypeConfiguration<BillingOperationRate>
{
    public void Configure(EntityTypeBuilder<BillingOperationRate> builder)
    {
        builder.ToTable("BillingOperationRates");

        builder.HasKey(rate => rate.OperationRateId);

        builder.Property(rate => rate.OperationRateId)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(rate => rate.RatePlanId)
            .IsRequired();

        builder.Property(rate => rate.Operation)
            .IsRequired()
            .HasMaxLength(128)
            .HasDefaultValue(string.Empty);

        builder.Property(rate => rate.UnitOfMeasure)
            .IsRequired()
            .HasMaxLength(64)
            .HasDefaultValue(string.Empty);

        builder.Property(rate => rate.Category)
            .HasMaxLength(64);

        builder.Property(rate => rate.Description)
            .HasMaxLength(256);

        builder.Property(rate => rate.Rate)
            .HasColumnType("decimal(18,6)");

        builder.Property(rate => rate.IsActive)
            .HasDefaultValue(true);

        builder.Property(rate => rate.CreatedUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(rate => rate.UpdatedUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(rate => rate.RatePlan)
            .WithMany(plan => plan.OperationRates)
            .HasForeignKey(rate => rate.RatePlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(rate => new { rate.RatePlanId, rate.Operation })
            .IsUnique()
            .HasDatabaseName("UX_BillingOperationRates_Active")
            .HasFilter("[IsActive] = 1");
    }
}

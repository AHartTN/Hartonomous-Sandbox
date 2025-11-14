using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class BillingOperationRateConfiguration : IEntityTypeConfiguration<BillingOperationRate>
{
    public void Configure(EntityTypeBuilder<BillingOperationRate> builder)
    {
        builder.ToTable("BillingOperationRates", "dbo");
        builder.HasKey(e => new { e.OperationRateId });

        builder.Property(e => e.OperationRateId)
            .HasColumnType("uniqueidentifier")
            ;

        builder.Property(e => e.Category)
            .HasColumnType("nvarchar(64)")
            .HasMaxLength(64)
            ;

        builder.Property(e => e.CreatedUtc)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Description)
            .HasColumnType("nvarchar(256)")
            .HasMaxLength(256)
            ;

        builder.Property(e => e.IsActive)
            .HasColumnType("bit")
            ;

        builder.Property(e => e.Operation)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            .IsRequired()
            ;

        builder.Property(e => e.Rate)
            .HasColumnType("decimal(18,6)")
            ;

        builder.Property(e => e.RatePlanId)
            .HasColumnType("uniqueidentifier")
            ;

        builder.Property(e => e.UnitOfMeasure)
            .HasColumnType("nvarchar(64)")
            .HasMaxLength(64)
            .IsRequired()
            ;

        builder.Property(e => e.UpdatedUtc)
            .HasColumnType("datetime2")
            ;

        builder.HasOne(d => d.RatePlan)
            .WithMany(p => p.BillingOperationRates)
            .HasForeignKey(d => new { d.RatePlanId })
            ;

        builder.HasIndex(e => new { e.RatePlanId, e.Operation })
            .HasDatabaseName("UX_BillingOperationRates_Active")
            .IsUnique()
            ;
    }
}

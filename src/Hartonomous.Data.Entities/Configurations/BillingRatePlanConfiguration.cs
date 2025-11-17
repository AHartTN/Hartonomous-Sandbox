using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class BillingRatePlanConfiguration : IEntityTypeConfiguration<BillingRatePlan>
{
    public void Configure(EntityTypeBuilder<BillingRatePlan> builder)
    {
        builder.ToTable("BillingRatePlan", "dbo");
        builder.HasKey(e => new { e.RatePlanId });

        builder.Property(e => e.RatePlanId)
            .HasColumnType("uniqueidentifier")
            ;

        builder.Property(e => e.AllowsPrivateData)
            .HasColumnType("bit")
            ;

        builder.Property(e => e.CanQueryPublicCorpus)
            .HasColumnType("bit")
            ;

        builder.Property(e => e.CreatedUtc)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.DefaultRate)
            .HasColumnType("decimal(18,6)")
            ;

        builder.Property(e => e.Description)
            .HasColumnType("nvarchar(256)")
            .HasMaxLength(256)
            ;

        builder.Property(e => e.IncludedPrivateStorageGb)
            .HasColumnType("decimal(18,2)")
            ;

        builder.Property(e => e.IncludedPublicStorageGb)
            .HasColumnType("decimal(18,2)")
            ;

        builder.Property(e => e.IncludedSeatCount)
            .HasColumnType("int")
            ;

        builder.Property(e => e.IsActive)
            .HasColumnType("bit")
            ;

        builder.Property(e => e.MonthlyFee)
            .HasColumnType("decimal(18,2)")
            ;

        builder.Property(e => e.Name)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            .IsRequired()
            ;

        builder.Property(e => e.PlanCode)
            .HasColumnType("nvarchar(64)")
            .HasMaxLength(64)
            .IsRequired()
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("nvarchar(64)")
            .HasMaxLength(64)
            ;

        builder.Property(e => e.UnitPricePerDcu)
            .HasColumnType("decimal(18,6)")
            ;

        builder.Property(e => e.UpdatedUtc)
            .HasColumnType("datetime2")
            ;
    }
}

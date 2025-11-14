using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class BillingUsageLedgerConfiguration : IEntityTypeConfiguration<BillingUsageLedger>
{
    public void Configure(EntityTypeBuilder<BillingUsageLedger> builder)
    {
        builder.ToTable("BillingUsageLedger", "dbo");
        builder.HasKey(e => new { e.LedgerId });

        builder.Property(e => e.LedgerId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.BaseRate)
            .HasColumnType("decimal(18,6)")
            ;

        builder.Property(e => e.CostPerUnit)
            .HasColumnType("decimal(18,8)")
            ;

        builder.Property(e => e.Handler)
            .HasColumnType("nvarchar(256)")
            .HasMaxLength(256)
            ;

        builder.Property(e => e.MessageType)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            ;

        builder.Property(e => e.Metadata)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.MetadataJson)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.Multiplier)
            .HasColumnType("decimal(18,6)")
            ;

        builder.Property(e => e.Operation)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            ;

        builder.Property(e => e.PrincipalId)
            .HasColumnType("nvarchar(256)")
            .HasMaxLength(256)
            ;

        builder.Property(e => e.Quantity)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.RecordedUtc)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            .IsRequired()
            ;

        builder.Property(e => e.TimestampUtc)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.TotalCost)
            .HasColumnType("decimal(18,6)")
            ;

        builder.Property(e => e.UnitType)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            ;

        builder.Property(e => e.Units)
            .HasColumnType("decimal(18,6)")
            ;

        builder.Property(e => e.UsageType)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            ;

        builder.HasIndex(e => new { e.Operation, e.TimestampUtc })
            .HasDatabaseName("IX_BillingUsageLedger_Operation_Timestamp")
            ;

        builder.HasIndex(e => new { e.TenantId, e.TimestampUtc })
            .HasDatabaseName("IX_BillingUsageLedger_Tenant")
            ;

        builder.HasIndex(e => new { e.TenantId, e.TimestampUtc })
            .HasDatabaseName("IX_BillingUsageLedger_TenantId_Timestamp")
            ;

        builder.HasIndex(e => new { e.UsageType, e.RecordedUtc })
            .HasDatabaseName("IX_BillingUsageLedger_UsageType")
            ;
    }
}

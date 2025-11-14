using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class BillingUsageLedgerInMemoryConfiguration : IEntityTypeConfiguration<BillingUsageLedgerInMemory>
{
    public void Configure(EntityTypeBuilder<BillingUsageLedgerInMemory> builder)
    {
        builder.ToTable("BillingUsageLedger_InMemory", "dbo");
        builder.HasKey(e => new { e.LedgerId });

        builder.Property(e => e.LedgerId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.BaseRate)
            .HasColumnType("decimal(18,6)")
            ;

        builder.Property(e => e.Handler)
            .HasColumnType("nvarchar(256)")
            .HasMaxLength(256)
            ;

        builder.Property(e => e.MessageType)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
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
            .IsRequired()
            ;

        builder.Property(e => e.PrincipalId)
            .HasColumnType("nvarchar(256)")
            .HasMaxLength(256)
            .IsRequired()
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

        builder.Property(e => e.Units)
            .HasColumnType("decimal(18,6)")
            ;

        builder.HasIndex(e => new { e.TenantId })
            .HasDatabaseName("IX_TenantId_Hash")
            ;

        builder.HasIndex(e => new { e.TimestampUtc })
            .HasDatabaseName("IX_Timestamp_Range")
            ;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Entities.Configurations;

public class BillingInvoiceConfiguration : IEntityTypeConfiguration<BillingInvoice>
{
    public void Configure(EntityTypeBuilder<BillingInvoice> builder)
    {
        builder.ToTable("BillingInvoice", "dbo");
        builder.HasKey(e => new { e.InvoiceId });

        builder.Property(e => e.InvoiceId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.BillingPeriodEnd)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.BillingPeriodStart)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Discount)
            .HasColumnType("decimal(18,2)")
            ;

        builder.Property(e => e.GeneratedUtc)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.InvoiceNumber)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired()
            ;

        builder.Property(e => e.MetadataJson)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.PaidUtc)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Status)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.Property(e => e.StripeHostedUrl)
            .HasColumnType("nvarchar(500)")
            .HasMaxLength(500)
            ;

        builder.Property(e => e.StripeInvoiceId)
            .HasColumnType("nvarchar(255)")
            .HasMaxLength(255)
            ;

        builder.Property(e => e.StripePdfUrl)
            .HasColumnType("nvarchar(500)")
            .HasMaxLength(500)
            ;

        builder.Property(e => e.StripeStatus)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            ;

        builder.Property(e => e.Subtotal)
            .HasColumnType("decimal(18,2)")
            ;

        builder.Property(e => e.Tax)
            .HasColumnType("decimal(18,2)")
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.Total)
            .HasColumnType("decimal(18,2)")
            ;

        builder.HasIndex(e => new { e.Status, e.GeneratedUtc })
            .HasDatabaseName("IX_BillingInvoices_Status")
            ;

        builder.HasIndex(e => new { e.StripeInvoiceId })
            .HasDatabaseName("IX_BillingInvoices_StripeId")
            ;

        builder.HasIndex(e => new { e.TenantId, e.GeneratedUtc })
            .HasDatabaseName("IX_BillingInvoices_Tenant")
            ;

        builder.HasIndex(e => new { e.InvoiceNumber })
            .HasDatabaseName("UQ_BillingInvoices_Number")
            .IsUnique()
            ;

        builder.HasIndex(e => new { e.StripeInvoiceId })
            .HasDatabaseName("UQ_BillingInvoices_StripeId")
            .IsUnique()
            ;
    }
}

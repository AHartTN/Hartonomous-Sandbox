using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Entities.Configurations;

public class TenantGuidMappingConfiguration : IEntityTypeConfiguration<TenantGuidMapping>
{
    public void Configure(EntityTypeBuilder<TenantGuidMapping> builder)
    {
        builder.ToTable("TenantGuidMapping", "dbo");
        builder.HasKey(e => new { e.TenantId });

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.CreatedBy)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            ;

        builder.Property(e => e.IsActive)
            .HasColumnType("bit")
            ;

        builder.Property(e => e.ModifiedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.ModifiedBy)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            ;

        builder.Property(e => e.TenantGuid)
            .HasColumnType("uniqueidentifier")
            ;

        builder.Property(e => e.TenantName)
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired()
            ;

        builder.HasIndex(e => new { e.IsActive })
            .HasDatabaseName("IX_TenantGuidMapping_IsActive")
            ;

        builder.HasIndex(e => new { e.TenantGuid })
            .HasDatabaseName("UQ_TenantGuidMapping_TenantGuid")
            .IsUnique()
            ;
    }
}

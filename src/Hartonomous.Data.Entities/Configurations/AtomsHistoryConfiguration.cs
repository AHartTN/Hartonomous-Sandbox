using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AtomsHistoryConfiguration : IEntityTypeConfiguration<AtomsHistory>
{
    public void Configure(EntityTypeBuilder<AtomsHistory> builder)
    {
        builder.ToTable("AtomsHistory", "dbo");

        builder.Property(e => e.AtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.AtomicValue)
            .HasColumnType("varbinary(64)")
            .HasMaxLength(64)
            ;

        builder.Property(e => e.CanonicalText)
            .HasColumnType("nvarchar(256)")
            .HasMaxLength(256)
            ;

        builder.Property(e => e.ContentHash)
            .HasColumnType("binary(32)")
            .HasMaxLength(32)
            .IsRequired()
            ;

        builder.Property(e => e.ContentType)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.CreatedUtc)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.IsActive)
            .HasColumnType("bit")
            ;

        builder.Property(e => e.IsDeleted)
            .HasColumnType("bit")
            ;

        builder.Property(e => e.Modality)
            .HasColumnType("nvarchar(64)")
            .HasMaxLength(64)
            .IsRequired()
            ;

        builder.Property(e => e.ReferenceCount)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.SourceType)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            ;

        builder.Property(e => e.SourceUri)
            .HasColumnType("nvarchar(1024)")
            .HasMaxLength(1024)
            ;

        builder.Property(e => e.SpatialGeography)
            .HasColumnType("geography")
            ;

        builder.Property(e => e.SpatialKey)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.Subtype)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.UpdatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.ValidFrom)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.ValidTo)
            .HasColumnType("datetime2")
            ;

        builder.HasIndex(e => new { e.ContentHash, e.ValidFrom, e.ValidTo })
            .HasDatabaseName("IX_AtomsHistory_ContentHash")
            ;

        builder.HasIndex(e => new { e.ValidFrom, e.ValidTo })
            .HasDatabaseName("IX_AtomsHistory_Period")
            ;
    }
}

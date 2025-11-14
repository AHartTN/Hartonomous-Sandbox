using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AtomConfiguration : IEntityTypeConfiguration<Atom>
{
    public void Configure(EntityTypeBuilder<Atom> builder)
    {
        builder.ToTable("Atoms", "dbo");
        builder.HasKey(e => new { e.AtomId });

        builder.Property(e => e.AtomId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.AtomicValue)
            .HasColumnType("varbinary(64)")
            .HasMaxLength(64)
            ;

        builder.Property(e => e.CanonicalText)
            .HasColumnType("nvarchar(256)")
            .HasMaxLength(256)
            ;

        builder.Property(e => e.ComponentStream)
            .HasColumnType("varbinary(max)")
            ;

        builder.Property(e => e.Content)
            .HasColumnType("nvarchar(max)")
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

        builder.Property(e => e.Metadata)
            .HasColumnType("json")
            ;

        builder.Property(e => e.Modality)
            .HasColumnType("nvarchar(64)")
            .HasMaxLength(64)
            .IsRequired()
            ;

        builder.Property(e => e.PayloadLocator)
            .HasColumnType("nvarchar(1024)")
            .HasMaxLength(1024)
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

        builder.HasIndex(e => new { e.Modality, e.Subtype })
            .HasDatabaseName("IX_Atoms_Modality_Subtype")
            ;

        builder.HasIndex(e => new { e.ReferenceCount })
            .HasDatabaseName("IX_Atoms_References")
            ;

        builder.HasIndex(e => new { e.SpatialKey })
            .HasDatabaseName("IX_Atoms_SpatialKey")
            ;

        builder.HasIndex(e => new { e.TenantId, e.IsActive, e.IsDeleted })
            .HasDatabaseName("IX_Atoms_TenantActive")
            ;

        builder.HasIndex(e => new { e.ContentHash })
            .HasDatabaseName("UQ_Atoms_ContentHash")
            .IsUnique()
            ;

        builder.HasIndex(e => new { e.ContentHash })
            .HasDatabaseName("UX_Atoms_ContentHash")
            .IsUnique()
            ;
    }
}

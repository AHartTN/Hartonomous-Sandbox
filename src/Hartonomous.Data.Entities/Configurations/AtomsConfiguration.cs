using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AtomsConfiguration : IEntityTypeConfiguration<Atoms>
{
    public void Configure(EntityTypeBuilder<Atoms> builder)
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
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.ContentHash)
            .HasColumnType("binary(32)")
            .HasMaxLength(32)
            .IsRequired()
            ;

        builder.Property(e => e.ContentType)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            ;

        builder.Property(e => e.Metadata)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.Modality)
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.Property(e => e.ReferenceCount)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.SourceType)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            ;

        builder.Property(e => e.SourceUri)
            .HasColumnType("nvarchar(2048)")
            .HasMaxLength(2048)
            ;

        builder.Property(e => e.Subtype)
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            ;

        builder.HasIndex(e => new { e.ContentType })
            .HasDatabaseName("IX_Atoms_ContentType")
            ;

        builder.HasIndex(e => new { e.Modality, e.Subtype })
            .HasDatabaseName("IX_Atoms_Modality")
            ;

        builder.HasIndex(e => new { e.ReferenceCount })
            .HasDatabaseName("IX_Atoms_ReferenceCount")
            ;

        builder.HasIndex(e => new { e.TenantId, e.Modality })
            .HasDatabaseName("IX_Atoms_TenantId")
            ;

        builder.HasIndex(e => new { e.ContentHash })
            .HasDatabaseName("UX_Atoms_ContentHash")
            .IsUnique()
            ;
    }
}

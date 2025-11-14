using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AtomPayloadStoreConfiguration : IEntityTypeConfiguration<AtomPayloadStore>
{
    public void Configure(EntityTypeBuilder<AtomPayloadStore> builder)
    {
        builder.ToTable("AtomPayloadStore", "dbo");
        builder.HasKey(e => new { e.PayloadId });

        builder.Property(e => e.PayloadId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.AtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.ContentHash)
            .HasColumnType("binary(32)")
            .HasMaxLength(32)
            .IsRequired()
            ;

        builder.Property(e => e.ContentType)
            .HasColumnType("nvarchar(256)")
            .HasMaxLength(256)
            .IsRequired()
            ;

        builder.Property(e => e.CreatedBy)
            .HasColumnType("nvarchar(256)")
            .HasMaxLength(256)
            ;

        builder.Property(e => e.CreatedUtc)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.PayloadData)
            .HasColumnType("varbinary(max)")
            .IsRequired()
            ;

        builder.Property(e => e.RowGuid)
            .HasColumnType("uniqueidentifier")
            ;

        builder.Property(e => e.SizeBytes)
            .HasColumnType("bigint")
            ;

        builder.HasOne(d => d.Atom)
            .WithMany(p => p.AtomPayloadStores)
            .HasForeignKey(d => new { d.AtomId })
            ;

        builder.HasIndex(e => new { e.AtomId })
            .HasDatabaseName("IX_AtomPayloadStore_AtomId")
            ;

        builder.HasIndex(e => new { e.RowGuid })
            .HasDatabaseName("IX_AtomPayloadStore_RowGuid")
            ;

        builder.HasIndex(e => new { e.RowGuid })
            .HasDatabaseName("UQ_AtomPayloadStore_RowGuid")
            .IsUnique()
            ;

        builder.HasIndex(e => new { e.ContentHash })
            .HasDatabaseName("UX_AtomPayloadStore_ContentHash")
            .IsUnique()
            ;
    }
}

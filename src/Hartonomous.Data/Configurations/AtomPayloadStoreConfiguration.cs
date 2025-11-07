using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public class AtomPayloadStoreConfiguration : IEntityTypeConfiguration<AtomPayloadStore>
{
    public void Configure(EntityTypeBuilder<AtomPayloadStore> builder)
    {
        builder.ToTable("AtomPayloadStore");

        builder.HasKey(e => e.PayloadId);

        builder.Property(e => e.PayloadId)
            .UseIdentityColumn();

        builder.Property(e => e.RowGuid)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(e => e.CreatedUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // FILESTREAM configuration
        builder.Property(e => e.PayloadData)
            .HasColumnType("VARBINARY(MAX) FILESTREAM");

        // Indexes
        builder.HasIndex(e => e.AtomId)
            .HasDatabaseName("IX_AtomPayloadStore_AtomId");

        builder.HasIndex(e => e.ContentHash)
            .HasDatabaseName("IX_AtomPayloadStore_ContentHash");

        builder.HasIndex(e => e.RowGuid)
            .HasDatabaseName("IX_AtomPayloadStore_RowGuid");

        // Unique constraint for deduplication
        builder.HasIndex(e => e.ContentHash)
            .IsUnique()
            .HasDatabaseName("UX_AtomPayloadStore_ContentHash");

        // Foreign key
        builder.HasOne(e => e.Atom)
            .WithMany()
            .HasForeignKey(e => e.AtomId)
            .OnDelete(DeleteBehavior.Cascade);

        // Check constraints
        builder.ToTable(t => {
            t.HasCheckConstraint("CK_AtomPayloadStore_ContentType", "[ContentType] LIKE '%/%'");
            t.HasCheckConstraint("CK_AtomPayloadStore_SizeBytes", "[SizeBytes] > 0");
        });
    }
}
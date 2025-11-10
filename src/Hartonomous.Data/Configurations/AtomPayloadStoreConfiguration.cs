using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public class AtomPayloadStoreConfiguration : IEntityTypeConfiguration<AtomPayloadStore>
{
    public void Configure(EntityTypeBuilder<AtomPayloadStore> builder)
    {
        builder.ToTable("AtomPayloadStore", t => 
        {
            t.HasCheckConstraint("CK_AtomPayloadStore_ContentType", "[ContentType] LIKE '%/%'");
            t.HasCheckConstraint("CK_AtomPayloadStore_SizeBytes", "[SizeBytes] > 0");
        });

        // FILESTREAM tables must be created on the FILESTREAM filegroup
        builder.ToTable(tb => tb.UseSqlOutputClause(false));  // FILESTREAM tables don't support OUTPUT clause

        builder.HasKey(e => e.PayloadId);

        builder.Property(e => e.PayloadId)
            .UseIdentityColumn();

        // FILESTREAM requires ROWGUIDCOL with UNIQUE constraint
        builder.Property(e => e.RowGuid)
            .HasColumnType("uniqueidentifier ROWGUIDCOL")
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(e => e.CreatedUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // FILESTREAM configuration
        builder.Property(e => e.PayloadData)
            .HasColumnType("VARBINARY(MAX) FILESTREAM");

        // ROWGUIDCOL must have UNIQUE CONSTRAINT (not just an index)
        builder.HasAlternateKey(e => e.RowGuid)
            .HasName("UX_AtomPayloadStore_RowGuid");

        // Indexes
        builder.HasIndex(e => e.AtomId)
            .HasDatabaseName("IX_AtomPayloadStore_AtomId");

        builder.HasIndex(e => e.ContentHash)
            .HasDatabaseName("IX_AtomPayloadStore_ContentHash");

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
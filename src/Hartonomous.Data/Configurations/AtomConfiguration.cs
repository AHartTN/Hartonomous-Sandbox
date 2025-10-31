using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public class AtomConfiguration : IEntityTypeConfiguration<Atom>
{
    public void Configure(EntityTypeBuilder<Atom> builder)
    {
        builder.ToTable("Atoms");

        builder.HasKey(a => a.AtomId);

        builder.Property(a => a.ContentHash)
            .IsRequired()
            .HasColumnType("binary(32)");

        builder.HasIndex(a => a.ContentHash)
            .IsUnique()
            .HasDatabaseName("UX_Atoms_ContentHash");

        builder.Property(a => a.Modality)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(a => a.Subtype)
            .HasMaxLength(128);

        builder.Property(a => a.SourceUri)
            .HasMaxLength(1024);

        builder.Property(a => a.SourceType)
            .HasMaxLength(128);

        builder.Property(a => a.PayloadLocator)
            .HasMaxLength(1024);

        builder.Property(a => a.Metadata)
            .HasColumnType("JSON");

        builder.Property(a => a.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(a => a.IsActive)
            .HasDefaultValue(true);

        builder.Property(a => a.ReferenceCount)
            .HasDefaultValue(0L);

        builder.Property(a => a.SpatialKey)
            .HasColumnType("geometry");

        builder.HasMany(a => a.Embeddings)
            .WithOne(e => e.Atom)
            .HasForeignKey(e => e.AtomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.TensorAtoms)
            .WithOne(t => t.Atom)
            .HasForeignKey(t => t.AtomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.SourceRelations)
            .WithOne(r => r.SourceAtom)
            .HasForeignKey(r => r.SourceAtomId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(a => a.TargetRelations)
            .WithOne(r => r.TargetAtom)
            .HasForeignKey(r => r.TargetAtomId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

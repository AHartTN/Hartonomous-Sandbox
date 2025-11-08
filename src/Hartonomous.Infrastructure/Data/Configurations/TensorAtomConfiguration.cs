using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Infrastructure.Data.Configurations;

public class TensorAtomConfiguration : IEntityTypeConfiguration<TensorAtom>
{
    public void Configure(EntityTypeBuilder<TensorAtom> builder)
    {
        builder.ToTable("TensorAtoms");

        builder.HasKey(t => t.TensorAtomId);

        builder.Property(t => t.AtomType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(t => t.Metadata)
            .HasColumnType("JSON");

        builder.Property(t => t.SpatialSignature)
            .HasColumnType("geometry");

        builder.Property(t => t.GeometryFootprint)
            .HasColumnType("geometry");

        builder.Property(t => t.ImportanceScore)
            .HasColumnType("real");

        builder.Property(t => t.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(t => new { t.ModelId, t.LayerId, t.AtomType })
            .HasDatabaseName("IX_TensorAtoms_Model_Layer_Type");

        builder.HasOne(t => t.Atom)
            .WithMany(a => a.TensorAtoms)
            .HasForeignKey(t => t.AtomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Model)
            .WithMany(m => m.TensorAtoms)
            .HasForeignKey(t => t.ModelId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(t => t.Layer)
            .WithMany(l => l.TensorAtoms)
            .HasForeignKey(t => t.LayerId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

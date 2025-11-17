using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Entities.Configurations;

public class TensorAtomConfiguration : IEntityTypeConfiguration<TensorAtom>
{
    public void Configure(EntityTypeBuilder<TensorAtom> builder)
    {
        builder.ToTable("TensorAtom", "dbo");
        builder.HasKey(e => new { e.TensorAtomId });

        builder.Property(e => e.TensorAtomId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.AtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.AtomType)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            .IsRequired()
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.GeometryFootprint)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.ImportanceScore)
            .HasColumnType("real")
            ;

        builder.Property(e => e.LayerId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.Metadata)
            .HasColumnType("json")
            ;

        builder.Property(e => e.ModelId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.SpatialSignature)
            .HasColumnType("geometry")
            ;

        builder.HasOne(d => d.Atom)
            .WithMany(p => p.TensorAtoms)
            .HasForeignKey(d => new { d.AtomId })
            ;

        builder.HasOne(d => d.Layer)
            .WithMany(p => p.TensorAtoms)
            .HasForeignKey(d => new { d.LayerId })
            .OnDelete(DeleteBehavior.ClientSetNull)
            ;

        builder.HasOne(d => d.Model)
            .WithMany(p => p.TensorAtoms)
            .HasForeignKey(d => new { d.ModelId })
            .OnDelete(DeleteBehavior.ClientSetNull)
            ;
    }
}

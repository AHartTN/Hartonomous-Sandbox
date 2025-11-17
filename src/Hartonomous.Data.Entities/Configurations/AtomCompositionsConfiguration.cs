using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AtomCompositionsConfiguration : IEntityTypeConfiguration<AtomCompositions>
{
    public void Configure(EntityTypeBuilder<AtomCompositions> builder)
    {
        builder.ToTable("AtomCompositions", "dbo");
        builder.HasKey(e => new { e.CompositionId });

        builder.Property(e => e.CompositionId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.ComponentAtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.ParentAtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.SequenceIndex)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.SpatialKey)
            .HasColumnType("geometry")
            ;

        builder.HasOne(d => d.ComponentAtom)
            .WithMany(p => p.AtomCompositionsComponentAtom)
            .HasForeignKey(d => new { d.ComponentAtomId })
            ;

        builder.HasOne(d => d.ParentAtom)
            .WithMany(p => p.AtomCompositionsParentAtom)
            .HasForeignKey(d => new { d.ParentAtomId })
            ;
    }
}

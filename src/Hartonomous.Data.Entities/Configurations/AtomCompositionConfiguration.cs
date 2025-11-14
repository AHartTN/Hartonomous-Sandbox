using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AtomCompositionConfiguration : IEntityTypeConfiguration<AtomComposition>
{
    public void Configure(EntityTypeBuilder<AtomComposition> builder)
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

        builder.Property(e => e.ComponentType)
            .HasColumnType("nvarchar(64)")
            .HasMaxLength(64)
            .IsRequired()
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.DimensionM)
            .HasColumnType("int")
            ;

        builder.Property(e => e.DimensionX)
            .HasColumnType("int")
            ;

        builder.Property(e => e.DimensionY)
            .HasColumnType("int")
            ;

        builder.Property(e => e.DimensionZ)
            .HasColumnType("int")
            ;

        builder.Property(e => e.Metadata)
            .HasColumnType("json")
            ;

        builder.Property(e => e.PositionKey)
            .HasColumnType("geometry")
            .IsRequired()
            ;

        builder.Property(e => e.SequenceIndex)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.SourceAtomId)
            .HasColumnType("bigint")
            ;

        builder.HasOne(d => d.ComponentAtom)
            .WithMany(p => p.AtomCompositionComponentAtoms)
            .HasForeignKey(d => new { d.ComponentAtomId })
            ;

        builder.HasOne(d => d.SourceAtom)
            .WithMany(p => p.AtomCompositionSourceAtoms)
            .HasForeignKey(d => new { d.SourceAtomId })
            ;

        builder.HasIndex(e => new { e.ComponentAtomId })
            .HasDatabaseName("IX_AtomCompositions_Component")
            ;

        builder.HasIndex(e => new { e.SourceAtomId })
            .HasDatabaseName("IX_AtomCompositions_Source")
            ;

        builder.HasIndex(e => new { e.ComponentType, e.SourceAtomId })
            .HasDatabaseName("IX_AtomCompositions_Type")
            ;

        builder.HasIndex(e => new { e.PositionKey })
            .HasDatabaseName("SIDX_AtomCompositions_Position")
            ;
    }
}

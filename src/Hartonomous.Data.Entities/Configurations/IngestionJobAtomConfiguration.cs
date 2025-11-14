using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class IngestionJobAtomConfiguration : IEntityTypeConfiguration<IngestionJobAtom>
{
    public void Configure(EntityTypeBuilder<IngestionJobAtom> builder)
    {
        builder.ToTable("IngestionJobAtoms", "dbo");
        builder.HasKey(e => new { e.IngestionJobAtomId });

        builder.Property(e => e.IngestionJobAtomId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.AtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.IngestionJobId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.Notes)
            .HasColumnType("nvarchar(1024)")
            .HasMaxLength(1024)
            ;

        builder.Property(e => e.WasDuplicate)
            .HasColumnType("bit")
            ;

        builder.HasOne(d => d.Atom)
            .WithMany(p => p.IngestionJobAtoms)
            .HasForeignKey(d => new { d.AtomId })
            ;

        builder.HasOne(d => d.IngestionJob)
            .WithMany(p => p.IngestionJobAtoms)
            .HasForeignKey(d => new { d.IngestionJobId })
            ;

        builder.HasIndex(e => new { e.AtomId })
            .HasDatabaseName("IX_IngestionJobAtoms_AtomId")
            ;

        builder.HasIndex(e => new { e.IngestionJobId, e.AtomId })
            .HasDatabaseName("IX_IngestionJobAtoms_Job_Atom")
            ;
    }
}

using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Infrastructure.Data.Configurations;

public class IngestionJobAtomConfiguration : IEntityTypeConfiguration<IngestionJobAtom>
{
    public void Configure(EntityTypeBuilder<IngestionJobAtom> builder)
    {
        builder.ToTable("IngestionJobAtoms");

        builder.HasKey(ja => ja.IngestionJobAtomId);

        builder.Property(ja => ja.Notes)
            .HasMaxLength(1024);

        builder.HasIndex(ja => new { ja.IngestionJobId, ja.AtomId })
            .HasDatabaseName("IX_IngestionJobAtoms_Job_Atom");

        builder.HasOne(ja => ja.IngestionJob)
            .WithMany(j => j.JobAtoms)
            .HasForeignKey(ja => ja.IngestionJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ja => ja.Atom)
            .WithMany()
            .HasForeignKey(ja => ja.AtomId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

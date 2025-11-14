using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AtomsLobConfiguration : IEntityTypeConfiguration<AtomsLob>
{
    public void Configure(EntityTypeBuilder<AtomsLob> builder)
    {
        builder.ToTable("AtomsLOB", "dbo");
        builder.HasKey(e => new { e.AtomId });

        builder.Property(e => e.AtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.ComponentStream)
            .HasColumnType("varbinary(max)")
            ;

        builder.Property(e => e.Content)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Metadata)
            .HasColumnType("json")
            ;

        builder.Property(e => e.PayloadLocator)
            .HasColumnType("nvarchar(1024)")
            .HasMaxLength(1024)
            ;

        builder.Property(e => e.UpdatedAt)
            .HasColumnType("datetime2")
            ;

        builder.HasOne(d => d.Atom)
            .WithOne(p => p.AtomsLob)
            .HasForeignKey<AtomsLob>(e => new { e.AtomId })
            ;

        builder.HasIndex(e => new { e.PayloadLocator })
            .HasDatabaseName("IX_AtomsLOB_PayloadLocator")
            ;
    }
}

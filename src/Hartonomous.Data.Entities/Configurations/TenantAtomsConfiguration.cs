using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class TenantAtomsConfiguration : IEntityTypeConfiguration<TenantAtoms>
{
    public void Configure(EntityTypeBuilder<TenantAtoms> builder)
    {
        builder.ToTable("TenantAtoms", "dbo");
        builder.HasKey(e => new { e.TenantId, e.AtomId });

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.AtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.HasOne(d => d.Atom)
            .WithMany(p => p.TenantAtoms)
            .HasForeignKey(d => new { d.AtomId })
            ;
    }
}

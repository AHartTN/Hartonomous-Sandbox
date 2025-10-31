using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public class TensorAtomCoefficientConfiguration : IEntityTypeConfiguration<TensorAtomCoefficient>
{
    public void Configure(EntityTypeBuilder<TensorAtomCoefficient> builder)
    {
        builder.ToTable("TensorAtomCoefficients");

        builder.HasKey(c => c.TensorAtomCoefficientId);

        builder.Property(c => c.Coefficient)
            .HasColumnType("real");

        builder.Property(c => c.TensorRole)
            .HasMaxLength(128);

        builder.HasIndex(c => new { c.TensorAtomId, c.ParentLayerId, c.TensorRole })
            .HasDatabaseName("IX_TensorAtomCoefficients_Lookup");

        builder.HasOne(c => c.TensorAtom)
            .WithMany(t => t.Coefficients)
            .HasForeignKey(c => c.TensorAtomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.ParentLayer)
            .WithMany(l => l.TensorAtomCoefficients)
            .HasForeignKey(c => c.ParentLayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

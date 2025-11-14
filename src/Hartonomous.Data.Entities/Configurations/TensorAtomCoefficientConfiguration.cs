using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class TensorAtomCoefficientConfiguration : IEntityTypeConfiguration<TensorAtomCoefficient>
{
    public void Configure(EntityTypeBuilder<TensorAtomCoefficient> builder)
    {
        builder.ToTable("TensorAtomCoefficients", "dbo");
        builder.HasKey(e => new { e.TensorAtomCoefficientId });

        builder.Property(e => e.TensorAtomCoefficientId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.Coefficient)
            .HasColumnType("real")
            ;

        builder.Property(e => e.ParentLayerId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.TensorAtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.TensorRole)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            ;

        builder.HasOne(d => d.ParentLayer)
            .WithMany(p => p.TensorAtomCoefficients)
            .HasForeignKey(d => new { d.ParentLayerId })
            ;

        builder.HasOne(d => d.TensorAtom)
            .WithMany(p => p.TensorAtomCoefficients)
            .HasForeignKey(d => new { d.TensorAtomId })
            ;

        builder.HasIndex(e => new { e.TensorAtomId, e.ParentLayerId, e.TensorRole })
            .HasDatabaseName("IX_TensorAtomCoefficients_Lookup")
            ;

        builder.HasIndex(e => new { e.ParentLayerId })
            .HasDatabaseName("IX_TensorAtomCoefficients_ParentLayerId")
            ;
    }
}

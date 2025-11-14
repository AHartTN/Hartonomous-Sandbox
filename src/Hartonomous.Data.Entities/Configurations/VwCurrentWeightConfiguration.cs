using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class VwCurrentWeightConfiguration : IEntityTypeConfiguration<VwCurrentWeight>
{
    public void Configure(EntityTypeBuilder<VwCurrentWeight> builder)
    {
        builder.ToTable("");

        builder.Property(e => e.AtomDescription)
            .HasColumnType("nvarchar(4000)")
            .HasMaxLength(4000)
            ;

        builder.Property(e => e.AtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.AtomSource)
            .HasColumnType("nvarchar(4000)")
            .HasMaxLength(4000)
            ;

        builder.Property(e => e.AtomType)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            .IsRequired()
            ;

        builder.Property(e => e.Coefficient)
            .HasColumnType("real")
            ;

        builder.Property(e => e.ImportanceScore)
            .HasColumnType("real")
            ;

        builder.Property(e => e.LastUpdated)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.LayerId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.ModelId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ParentLayerId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.TensorAtomCoefficientId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.TensorAtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.TensorRole)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            ;
    }
}

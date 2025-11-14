using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class VwWeightChangeHistoryConfiguration : IEntityTypeConfiguration<VwWeightChangeHistory>
{
    public void Configure(EntityTypeBuilder<VwWeightChangeHistory> builder)
    {
        builder.ToTable("");

        builder.Property(e => e.ChangedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Coefficient)
            .HasColumnType("real")
            ;

        builder.Property(e => e.CoefficientDelta)
            .HasColumnType("real")
            ;

        builder.Property(e => e.DurationSeconds)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ParentLayerId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.PreviousCoefficient)
            .HasColumnType("real")
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

        builder.Property(e => e.ValidUntil)
            .HasColumnType("datetime2")
            ;
    }
}

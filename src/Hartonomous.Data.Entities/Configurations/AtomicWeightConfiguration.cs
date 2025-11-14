using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AtomicWeightConfiguration : IEntityTypeConfiguration<AtomicWeight>
{
    public void Configure(EntityTypeBuilder<AtomicWeight> builder)
    {
        builder.ToTable("AtomicWeights", "dbo");
        builder.HasKey(e => new { e.WeightHash });

        builder.Property(e => e.WeightHash)
            .HasColumnType("binary(32)")
            .HasMaxLength(32)
            .IsRequired()
            ;

        builder.Property(e => e.FirstSeen)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.LastReferenced)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.ReferenceCount)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.ValuePoint)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.WeightBytes)
            .HasColumnType("varbinary(4)")
            .HasMaxLength(4)
            .IsRequired()
            ;

        builder.Property(e => e.WeightValue)
            .HasColumnType("real")
            ;

        builder.HasIndex(e => new { e.ReferenceCount })
            .HasDatabaseName("IX_AtomicWeights_References")
            ;

        builder.HasIndex(e => new { e.WeightValue })
            .HasDatabaseName("IX_AtomicWeights_Value")
            ;

        builder.HasIndex(e => new { e.ValuePoint })
            .HasDatabaseName("SIDX_AtomicWeights_Value")
            ;
    }
}

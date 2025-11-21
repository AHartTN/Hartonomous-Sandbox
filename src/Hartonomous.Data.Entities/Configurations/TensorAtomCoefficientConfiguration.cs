using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Entities.Configurations;

public class TensorAtomCoefficientConfiguration : IEntityTypeConfiguration<TensorAtomCoefficient>
{
    public void Configure(EntityTypeBuilder<TensorAtomCoefficient> builder)
    {
        builder.ToTable("TensorAtomCoefficient", "dbo");
        builder.HasKey(e => new { e.TensorAtomId, e.ModelId, e.LayerIdx, e.PositionX, e.PositionY, e.PositionZ });

        builder.Property(e => e.TensorAtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.ModelId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.LayerIdx)
            .HasColumnType("int")
            ;

        builder.Property(e => e.PositionX)
            .HasColumnType("int")
            ;

        builder.Property(e => e.PositionY)
            .HasColumnType("int")
            ;

        builder.Property(e => e.PositionZ)
            .HasColumnType("int")
            ;

        builder.Property(e => e.Coefficient)
            .HasColumnType("real")
            ;

        builder.Property(e => e.ParentLayerId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.SpatialKey)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.TensorAtomCoefficientId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.TensorRole)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            ;

        builder.HasOne(d => d.Model)
            .WithMany(p => p.TensorAtomCoefficients)
            .HasForeignKey(d => new { d.ModelId })
            ;

        builder.HasOne(d => d.TensorAtom)
            .WithMany(p => p.TensorAtomCoefficients)
            .HasForeignKey(d => new { d.TensorAtomId })
            ;

        builder.HasIndex(e => new { e.ModelId, e.LayerIdx })
            .HasDatabaseName("IX_TensorAtomCoefficient_ModelId_LayerIdx")
            ;

        builder.HasIndex(e => new { e.SpatialKey })
            .HasDatabaseName("SIX_TensorAtomCoefficients_SpatialKey")
            ;
    }
}

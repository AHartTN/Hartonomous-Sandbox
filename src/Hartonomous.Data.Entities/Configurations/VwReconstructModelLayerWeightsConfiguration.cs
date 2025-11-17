using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class VwReconstructModelLayerWeightsConfiguration : IEntityTypeConfiguration<VwReconstructModelLayerWeights>
{
    public void Configure(EntityTypeBuilder<VwReconstructModelLayerWeights> builder)
    {
        builder.ToTable("");

        builder.Property(e => e.LayerIdx)
            .HasColumnType("int")
            ;

        builder.Property(e => e.LayerName)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            ;

        builder.Property(e => e.ModelId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ModelName)
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired()
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

        builder.Property(e => e.WeightValueBinary)
            .HasColumnType("varbinary(64)")
            .HasMaxLength(64)
            ;
    }
}

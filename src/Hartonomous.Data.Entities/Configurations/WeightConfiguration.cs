using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class WeightConfiguration : IEntityTypeConfiguration<Weight>
{
    public void Configure(EntityTypeBuilder<Weight> builder)
    {
        builder.ToTable("Weights", "dbo");
        builder.HasKey(e => new { e.WeightId });

        builder.Property(e => e.WeightId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.Gradient)
            .HasColumnType("real")
            ;

        builder.Property(e => e.ImportanceScore)
            .HasColumnType("real")
            ;

        builder.Property(e => e.LastUpdated)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.LayerId)
            .HasColumnName("LayerID")
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.Momentum)
            .HasColumnType("real")
            ;

        builder.Property(e => e.NeuronIndex)
            .HasColumnType("int")
            ;

        builder.Property(e => e.UpdateCount)
            .HasColumnType("int")
            ;

        builder.Property(e => e.Value)
            .HasColumnType("real")
            ;

        builder.Property(e => e.WeightType)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.HasOne(d => d.Layer)
            .WithMany(p => p.Weights)
            .HasForeignKey(d => new { d.LayerId })
            ;

        builder.HasIndex(e => new { e.ImportanceScore })
            .HasDatabaseName("IX_Weights_Importance")
            ;

        builder.HasIndex(e => new { e.LayerId, e.NeuronIndex })
            .HasDatabaseName("IX_Weights_Layer")
            ;
    }
}

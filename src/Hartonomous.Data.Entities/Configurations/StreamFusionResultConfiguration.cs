using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Entities.Configurations;

public class StreamFusionResultConfiguration : IEntityTypeConfiguration<StreamFusionResult>
{
    public void Configure(EntityTypeBuilder<StreamFusionResult> builder)
    {
        builder.ToTable("StreamFusionResults", "dbo");
        builder.HasKey(e => new { e.Id });

        builder.Property(e => e.Id)
            .HasColumnType("int")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.ComponentCount)
            .HasColumnType("int")
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.DurationMs)
            .HasColumnType("int")
            ;

        builder.Property(e => e.FusedStream)
            .HasColumnType("varbinary(max)")
            ;

        builder.Property(e => e.FusionType)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.Property(e => e.StreamIds)
            .HasColumnType("json")
            .IsRequired()
            ;

        builder.Property(e => e.Weights)
            .HasColumnType("json")
            ;

        builder.HasIndex(e => new { e.CreatedAt })
            .HasDatabaseName("IX_StreamFusionResults_CreatedAt")
            ;

        builder.HasIndex(e => new { e.FusionType })
            .HasDatabaseName("IX_StreamFusionResults_FusionType")
            ;
    }
}

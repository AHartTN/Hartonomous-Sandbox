using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class StreamOrchestrationResultConfiguration : IEntityTypeConfiguration<StreamOrchestrationResult>
{
    public void Configure(EntityTypeBuilder<StreamOrchestrationResult> builder)
    {
        builder.ToTable("StreamOrchestrationResults", "dbo");
        builder.HasKey(e => new { e.Id });

        builder.Property(e => e.Id)
            .HasColumnType("int")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.AggregationLevel)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.Property(e => e.ComponentCount)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ComponentStream)
            .HasColumnType("varbinary(max)")
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.DurationMs)
            .HasColumnType("int")
            ;

        builder.Property(e => e.SensorType)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired()
            ;

        builder.Property(e => e.TimeWindowEnd)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.TimeWindowStart)
            .HasColumnType("datetime2")
            ;

        builder.HasIndex(e => new { e.CreatedAt })
            .HasDatabaseName("IX_StreamOrchestrationResults_CreatedAt")
            ;

        builder.HasIndex(e => new { e.SensorType })
            .HasDatabaseName("IX_StreamOrchestrationResults_SensorType")
            ;

        builder.HasIndex(e => new { e.TimeWindowStart, e.TimeWindowEnd })
            .HasDatabaseName("IX_StreamOrchestrationResults_TimeWindow")
            ;
    }
}

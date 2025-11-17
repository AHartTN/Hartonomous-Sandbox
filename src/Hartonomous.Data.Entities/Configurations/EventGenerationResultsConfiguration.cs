using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class EventGenerationResultsConfiguration : IEntityTypeConfiguration<EventGenerationResults>
{
    public void Configure(EntityTypeBuilder<EventGenerationResults> builder)
    {
        builder.ToTable("EventGenerationResults", "dbo");
        builder.HasKey(e => new { e.Id });

        builder.Property(e => e.Id)
            .HasColumnType("int")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.ClusteringMethod)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.DurationMs)
            .HasColumnType("int")
            ;

        builder.Property(e => e.EventType)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired()
            ;

        builder.Property(e => e.EventsGenerated)
            .HasColumnType("int")
            ;

        builder.Property(e => e.StreamId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.Threshold)
            .HasColumnType("float")
            ;

        builder.HasOne(d => d.Stream)
            .WithMany(p => p.EventGenerationResults)
            .HasForeignKey(d => new { d.StreamId })
            ;

        builder.HasIndex(e => new { e.CreatedAt })
            .HasDatabaseName("IX_EventGenerationResults_CreatedAt")
            ;

        builder.HasIndex(e => new { e.EventType })
            .HasDatabaseName("IX_EventGenerationResults_EventType")
            ;

        builder.HasIndex(e => new { e.StreamId })
            .HasDatabaseName("IX_EventGenerationResults_StreamId")
            ;
    }
}

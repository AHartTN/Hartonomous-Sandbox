using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class EventHubCheckpointConfiguration : IEntityTypeConfiguration<EventHubCheckpoint>
{
    public void Configure(EntityTypeBuilder<EventHubCheckpoint> builder)
    {
        builder.ToTable("EventHubCheckpoints", "dbo");
        builder.HasKey(e => new { e.CheckpointId });

        builder.Property(e => e.CheckpointId)
            .HasColumnType("uniqueidentifier")
            ;

        builder.Property(e => e.ConsumerGroup)
            .HasColumnType("nvarchar(256)")
            .HasMaxLength(256)
            .IsRequired()
            ;

        builder.Property(e => e.Etag)
            .HasColumnName("ETag")
            .HasColumnType("nvarchar(36)")
            .HasMaxLength(36)
            .IsRequired()
            ;

        builder.Property(e => e.EventHubName)
            .HasColumnType("nvarchar(256)")
            .HasMaxLength(256)
            .IsRequired()
            ;

        builder.Property(e => e.FullyQualifiedNamespace)
            .HasColumnType("nvarchar(256)")
            .HasMaxLength(256)
            .IsRequired()
            ;

        builder.Property(e => e.LastModifiedTimeUtc)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Offset)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.OwnerIdentifier)
            .HasColumnType("nvarchar(256)")
            .HasMaxLength(256)
            ;

        builder.Property(e => e.PartitionId)
            .HasColumnType("nvarchar(64)")
            .HasMaxLength(64)
            .IsRequired()
            ;

        builder.Property(e => e.SequenceNumber)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.UniqueKeyHash)
            .HasColumnType("varbinary(32)")
            .HasMaxLength(32)
            ;

        builder.HasIndex(e => new { e.OwnerIdentifier })
            .HasDatabaseName("IX_EventHubCheckpoints_Owner")
            ;

        builder.HasIndex(e => new { e.UniqueKeyHash })
            .HasDatabaseName("UX_EventHubCheckpoints_Composite")
            .IsUnique()
            ;
    }
}

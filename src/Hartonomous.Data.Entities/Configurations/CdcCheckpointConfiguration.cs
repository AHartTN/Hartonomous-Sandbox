using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Entities.Configurations;

public class CdcCheckpointConfiguration : IEntityTypeConfiguration<CdcCheckpoint>
{
    public void Configure(EntityTypeBuilder<CdcCheckpoint> builder)
    {
        builder.ToTable("CdcCheckpoint", "dbo");
        builder.HasKey(e => new { e.ConsumerGroup, e.PartitionId });

        builder.Property(e => e.ConsumerGroup)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired()
            ;

        builder.Property(e => e.PartitionId)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.Property(e => e.LastModified)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Offset)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.SequenceNumber)
            .HasColumnType("bigint")
            ;
    }
}

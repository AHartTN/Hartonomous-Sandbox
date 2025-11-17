using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Entities.Configurations;

public class Neo4jSyncLogConfiguration : IEntityTypeConfiguration<Neo4jSyncLog>
{
    public void Configure(EntityTypeBuilder<Neo4jSyncLog> builder)
    {
        builder.ToTable("Neo4jSyncLog", "dbo");
        builder.HasKey(e => new { e.LogId });

        builder.Property(e => e.LogId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.EntityId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.EntityType)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.Property(e => e.ErrorMessage)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.Response)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.RetryCount)
            .HasColumnType("int")
            ;

        builder.Property(e => e.SyncStatus)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.Property(e => e.SyncTimestamp)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.SyncType)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.HasIndex(e => new { e.EntityType, e.EntityId, e.SyncTimestamp })
            .HasDatabaseName("IX_Neo4jSyncLog_Entity")
            ;

        builder.HasIndex(e => new { e.SyncStatus, e.SyncTimestamp })
            .HasDatabaseName("IX_Neo4jSyncLog_Status")
            ;

        builder.HasIndex(e => new { e.SyncTimestamp })
            .HasDatabaseName("IX_Neo4jSyncLog_Timestamp")
            ;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Entities.Configurations;

public class BackgroundJobConfiguration : IEntityTypeConfiguration<BackgroundJob>
{
    public void Configure(EntityTypeBuilder<BackgroundJob> builder)
    {
        builder.ToTable("BackgroundJob", "dbo");
        builder.HasKey(e => new { e.JobId });

        builder.Property(e => e.JobId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.AttemptCount)
            .HasColumnType("int")
            ;

        builder.Property(e => e.CompletedAtUtc)
            .HasColumnType("datetime2(3)")
            ;

        builder.Property(e => e.CorrelationId)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            ;

        builder.Property(e => e.CreatedAtUtc)
            .HasColumnType("datetime2(3)")
            ;

        builder.Property(e => e.CreatedBy)
            .HasColumnType("nvarchar(256)")
            .HasMaxLength(256)
            ;

        builder.Property(e => e.ErrorMessage)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.ErrorStackTrace)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.JobType)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            .IsRequired()
            ;

        builder.Property(e => e.MaxRetries)
            .HasColumnType("int")
            ;

        builder.Property(e => e.Payload)
            .HasColumnType("json")
            ;

        builder.Property(e => e.Priority)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ResultData)
            .HasColumnType("json")
            ;

        builder.Property(e => e.ScheduledAtUtc)
            .HasColumnType("datetime2(3)")
            ;

        builder.Property(e => e.StartedAtUtc)
            .HasColumnType("datetime2(3)")
            ;

        builder.Property(e => e.Status)
            .HasColumnType("int")
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            ;

        builder.HasIndex(e => new { e.CorrelationId })
            .HasDatabaseName("IX_BackgroundJob_CorrelationId")
            ;

        builder.HasIndex(e => new { e.JobType, e.Status })
            .HasDatabaseName("IX_BackgroundJob_JobType_Status")
            ;

        builder.HasIndex(e => new { e.ScheduledAtUtc })
            .HasDatabaseName("IX_BackgroundJob_ScheduledAtUtc")
            ;

        builder.HasIndex(e => new { e.Status, e.Priority, e.CreatedAtUtc })
            .HasDatabaseName("IX_BackgroundJob_Status_Priority")
            ;

        builder.HasIndex(e => new { e.TenantId })
            .HasDatabaseName("IX_BackgroundJob_TenantId")
            ;
    }
}

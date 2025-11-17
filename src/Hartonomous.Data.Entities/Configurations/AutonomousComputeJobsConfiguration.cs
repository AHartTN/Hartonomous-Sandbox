using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AutonomousComputeJobsConfiguration : IEntityTypeConfiguration<AutonomousComputeJobs>
{
    public void Configure(EntityTypeBuilder<AutonomousComputeJobs> builder)
    {
        builder.ToTable("AutonomousComputeJobs", "dbo");
        builder.HasKey(e => new { e.JobId });

        builder.Property(e => e.JobId)
            .HasColumnType("uniqueidentifier")
            ;

        builder.Property(e => e.CompletedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.CurrentState)
            .HasColumnType("json")
            ;

        builder.Property(e => e.JobParameters)
            .HasColumnType("json")
            .IsRequired()
            ;

        builder.Property(e => e.JobType)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired()
            ;

        builder.Property(e => e.Results)
            .HasColumnType("json")
            ;

        builder.Property(e => e.Status)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.Property(e => e.UpdatedAt)
            .HasColumnType("datetime2")
            ;

        builder.HasIndex(e => new { e.JobType })
            .HasDatabaseName("IX_AutonomousComputeJobs_JobType")
            ;

        builder.HasIndex(e => new { e.Status, e.CreatedAt })
            .HasDatabaseName("IX_AutonomousComputeJobs_Status_CreatedAt")
            ;
    }
}

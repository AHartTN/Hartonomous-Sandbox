using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class CicdbuildsConfiguration : IEntityTypeConfiguration<Cicdbuilds>
{
    public void Configure(EntityTypeBuilder<Cicdbuilds> builder)
    {
        builder.ToTable("CICDBuilds", "dbo");
        builder.HasKey(e => new { e.BuildId });

        builder.Property(e => e.BuildId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.ArtifactUrl)
            .HasColumnType("nvarchar(500)")
            .HasMaxLength(500)
            ;

        builder.Property(e => e.BranchName)
            .HasColumnType("nvarchar(255)")
            .HasMaxLength(255)
            .IsRequired()
            ;

        builder.Property(e => e.BuildAgent)
            .HasColumnType("nvarchar(255)")
            .HasMaxLength(255)
            ;

        builder.Property(e => e.BuildLogs)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.BuildNumber)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            ;

        builder.Property(e => e.CodeCoverage)
            .HasColumnType("decimal(5,2)")
            ;

        builder.Property(e => e.CommitHash)
            .HasColumnType("nvarchar(40)")
            .HasMaxLength(40)
            .IsRequired()
            ;

        builder.Property(e => e.CompletedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.DeployedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.DeploymentStatus)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            ;

        builder.Property(e => e.DurationMs)
            .HasColumnType("int")
            ;

        builder.Property(e => e.StartedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Status)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.Property(e => e.TestsFailed)
            .HasColumnType("int")
            ;

        builder.Property(e => e.TestsPassed)
            .HasColumnType("int")
            ;

        builder.Property(e => e.TriggerType)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            ;

        builder.HasIndex(e => new { e.CommitHash })
            .HasDatabaseName("IX_CICDBuilds_CommitHash")
            ;

        builder.HasIndex(e => new { e.CreatedAt })
            .HasDatabaseName("IX_CICDBuilds_CreatedAt")
            ;

        builder.HasIndex(e => new { e.Status, e.StartedAt })
            .HasDatabaseName("IX_CICDBuilds_Status_StartedAt")
            ;
    }
}

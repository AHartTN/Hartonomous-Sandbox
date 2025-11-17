using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Entities.Configurations;

public class AutonomousImprovementHistoryConfiguration : IEntityTypeConfiguration<AutonomousImprovementHistory>
{
    public void Configure(EntityTypeBuilder<AutonomousImprovementHistory> builder)
    {
        builder.ToTable("AutonomousImprovementHistory", "dbo");
        builder.HasKey(e => new { e.ImprovementId });

        builder.Property(e => e.ImprovementId)
            .HasColumnType("uniqueidentifier")
            ;

        builder.Property(e => e.AnalysisResults)
            .HasColumnType("nvarchar(max)")
            .IsRequired()
            ;

        builder.Property(e => e.ChangeType)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.Property(e => e.CompletedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.ErrorMessage)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.EstimatedImpact)
            .HasColumnType("nvarchar(20)")
            .HasMaxLength(20)
            ;

        builder.Property(e => e.GeneratedCode)
            .HasColumnType("nvarchar(max)")
            .IsRequired()
            ;

        builder.Property(e => e.GitCommitHash)
            .HasColumnType("nvarchar(64)")
            .HasMaxLength(64)
            ;

        builder.Property(e => e.PerformanceDelta)
            .HasColumnType("decimal(10,4)")
            ;

        builder.Property(e => e.RiskLevel)
            .HasColumnType("nvarchar(20)")
            .HasMaxLength(20)
            .IsRequired()
            ;

        builder.Property(e => e.RolledBackAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.StartedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.SuccessScore)
            .HasColumnType("decimal(5,4)")
            ;

        builder.Property(e => e.TargetFile)
            .HasColumnType("nvarchar(512)")
            .HasMaxLength(512)
            .IsRequired()
            ;

        builder.Property(e => e.TestsFailed)
            .HasColumnType("int")
            ;

        builder.Property(e => e.TestsPassed)
            .HasColumnType("int")
            ;

        builder.Property(e => e.WasDeployed)
            .HasColumnType("bit")
            ;

        builder.Property(e => e.WasRolledBack)
            .HasColumnType("bit")
            ;
    }
}

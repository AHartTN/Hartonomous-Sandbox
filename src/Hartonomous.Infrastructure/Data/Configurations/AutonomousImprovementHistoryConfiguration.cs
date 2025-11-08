using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Infrastructure.Data.Configurations;

public class AutonomousImprovementHistoryConfiguration : IEntityTypeConfiguration<AutonomousImprovementHistory>
{
    public void Configure(EntityTypeBuilder<AutonomousImprovementHistory> builder)
    {
        builder.ToTable("AutonomousImprovementHistory");

        builder.HasKey(e => e.ImprovementId);

        builder.Property(e => e.ImprovementId)
            .HasDefaultValueSql("NEWID()");

        builder.Property(e => e.SuccessScore)
            .HasPrecision(5, 4);

        builder.Property(e => e.PerformanceDelta)
            .HasPrecision(10, 4);

        // Indexes for performance
        builder.HasIndex(e => e.StartedAt)
            .HasDatabaseName("IX_AutonomousImprovement_StartedAt")
            .IsDescending();

        builder.HasIndex(e => new { e.ChangeType, e.RiskLevel })
            .HasDatabaseName("IX_AutonomousImprovement_ChangeType_RiskLevel")
            .IncludeProperties(e => new { e.ErrorMessage, e.SuccessScore });

        builder.HasIndex(e => e.SuccessScore)
            .HasDatabaseName("IX_AutonomousImprovement_SuccessScore")
            .IsDescending()
            .HasFilter("[WasDeployed] = 1 AND [WasRolledBack] = 0");

        // Check constraints
        builder.ToTable(t => t.HasCheckConstraint("CK_AutonomousImprovement_SuccessScore",
            "[SuccessScore] >= 0 AND [SuccessScore] <= 1"));
    }
}

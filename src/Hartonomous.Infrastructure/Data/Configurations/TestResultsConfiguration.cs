using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Infrastructure.Data.Configurations;

public class TestResultsConfiguration : IEntityTypeConfiguration<TestResults>
{
    public void Configure(EntityTypeBuilder<TestResults> builder)
    {
        builder.ToTable("TestResults");

        builder.HasKey(e => e.TestResultId);

        builder.Property(e => e.TestResultId)
            .UseIdentityColumn();

        builder.Property(e => e.ExecutedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // Indexes
        builder.HasIndex(e => new { e.TestSuite, e.ExecutedAt })
            .HasDatabaseName("IX_TestResults_TestSuite_ExecutedAt")
            .IsDescending();

        builder.HasIndex(e => e.TestStatus)
            .HasDatabaseName("IX_TestResults_TestStatus");

        builder.HasIndex(e => new { e.TestCategory, e.ExecutedAt })
            .HasDatabaseName("IX_TestResults_TestCategory_ExecutedAt")
            .IsDescending();

        builder.HasIndex(e => e.ExecutionTimeMs)
            .HasDatabaseName("IX_TestResults_ExecutionTimeMs")
            .IsDescending();
    }
}

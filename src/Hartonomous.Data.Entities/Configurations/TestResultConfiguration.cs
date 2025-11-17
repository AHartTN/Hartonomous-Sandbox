using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class TestResultConfiguration : IEntityTypeConfiguration<TestResult>
{
    public void Configure(EntityTypeBuilder<TestResult> builder)
    {
        builder.ToTable("TestResult", "dbo");
        builder.HasKey(e => new { e.TestResultId });

        builder.Property(e => e.TestResultId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.CpuUsagePercent)
            .HasColumnType("float")
            ;

        builder.Property(e => e.Environment)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            ;

        builder.Property(e => e.ErrorMessage)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.ExecutedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.ExecutionTimeMs)
            .HasColumnType("float")
            ;

        builder.Property(e => e.MemoryUsageMb)
            .HasColumnName("MemoryUsageMB")
            .HasColumnType("float")
            ;

        builder.Property(e => e.StackTrace)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.TestCategory)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            ;

        builder.Property(e => e.TestName)
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired()
            ;

        builder.Property(e => e.TestOutput)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.TestStatus)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.Property(e => e.TestSuite)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired()
            ;
    }
}

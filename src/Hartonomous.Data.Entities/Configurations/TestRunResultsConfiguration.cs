using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class TestRunResultsConfiguration : IEntityTypeConfiguration<TestRunResults>
{
    public void Configure(EntityTypeBuilder<TestRunResults> builder)
    {
        builder.ToTable("TestRunResults", "dbo");

        builder.Property(e => e.Duration)
            .HasColumnType("decimal(10,3)")
            ;

        builder.Property(e => e.ExecutedAt)
            .HasColumnType("datetime")
            ;

        builder.Property(e => e.FailedTests)
            .HasColumnType("int")
            ;

        builder.Property(e => e.PassedTests)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ResultsXml)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.TotalTests)
            .HasColumnType("int")
            ;
    }
}

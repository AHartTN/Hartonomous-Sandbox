using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Entities.Configurations;

public class SelfConsistencyResultConfiguration : IEntityTypeConfiguration<SelfConsistencyResult>
{
    public void Configure(EntityTypeBuilder<SelfConsistencyResult> builder)
    {
        builder.ToTable("SelfConsistencyResults", "dbo");
        builder.HasKey(e => new { e.Id });

        builder.Property(e => e.Id)
            .HasColumnType("int")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.AgreementRatio)
            .HasColumnType("float")
            ;

        builder.Property(e => e.ConsensusAnswer)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.ConsensusMetrics)
            .HasColumnType("json")
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.DurationMs)
            .HasColumnType("int")
            ;

        builder.Property(e => e.NumSamples)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ProblemId)
            .HasColumnType("uniqueidentifier")
            ;

        builder.Property(e => e.Prompt)
            .HasColumnType("nvarchar(max)")
            .IsRequired()
            ;

        builder.Property(e => e.SampleData)
            .HasColumnType("json")
            ;

        builder.HasIndex(e => new { e.CreatedAt })
            .HasDatabaseName("IX_SelfConsistencyResults_CreatedAt")
            ;

        builder.HasIndex(e => new { e.ProblemId })
            .HasDatabaseName("IX_SelfConsistencyResults_ProblemId")
            ;
    }
}

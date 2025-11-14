using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class ReasoningChainConfiguration : IEntityTypeConfiguration<ReasoningChain>
{
    public void Configure(EntityTypeBuilder<ReasoningChain> builder)
    {
        builder.ToTable("ReasoningChains", "dbo");
        builder.HasKey(e => new { e.Id });

        builder.Property(e => e.Id)
            .HasColumnType("int")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.ChainData)
            .HasColumnType("json")
            ;

        builder.Property(e => e.CoherenceMetrics)
            .HasColumnType("json")
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.DurationMs)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ProblemId)
            .HasColumnType("uniqueidentifier")
            ;

        builder.Property(e => e.ReasoningType)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.Property(e => e.TotalSteps)
            .HasColumnType("int")
            ;

        builder.HasIndex(e => new { e.CreatedAt })
            .HasDatabaseName("IX_ReasoningChains_CreatedAt")
            ;

        builder.HasIndex(e => new { e.ProblemId })
            .HasDatabaseName("IX_ReasoningChains_ProblemId")
            ;
    }
}

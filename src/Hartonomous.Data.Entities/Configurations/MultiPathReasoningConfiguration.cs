using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class MultiPathReasoningConfiguration : IEntityTypeConfiguration<MultiPathReasoning>
{
    public void Configure(EntityTypeBuilder<MultiPathReasoning> builder)
    {
        builder.ToTable("MultiPathReasoning", "dbo");
        builder.HasKey(e => new { e.Id });

        builder.Property(e => e.Id)
            .HasColumnType("int")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.BasePrompt)
            .HasColumnType("nvarchar(max)")
            .IsRequired()
            ;

        builder.Property(e => e.BestPathId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.DurationMs)
            .HasColumnType("int")
            ;

        builder.Property(e => e.MaxDepth)
            .HasColumnType("int")
            ;

        builder.Property(e => e.NumPaths)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ProblemId)
            .HasColumnType("uniqueidentifier")
            ;

        builder.Property(e => e.ReasoningTree)
            .HasColumnType("json")
            ;

        builder.HasIndex(e => new { e.CreatedAt })
            .HasDatabaseName("IX_MultiPathReasoning_CreatedAt")
            ;

        builder.HasIndex(e => new { e.ProblemId })
            .HasDatabaseName("IX_MultiPathReasoning_ProblemId")
            ;
    }
}

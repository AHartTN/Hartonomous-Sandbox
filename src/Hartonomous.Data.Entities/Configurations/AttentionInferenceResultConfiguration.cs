using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AttentionInferenceResultConfiguration : IEntityTypeConfiguration<AttentionInferenceResult>
{
    public void Configure(EntityTypeBuilder<AttentionInferenceResult> builder)
    {
        builder.ToTable("AttentionInferenceResults", "dbo");
        builder.HasKey(e => new { e.Id });

        builder.Property(e => e.Id)
            .HasColumnType("int")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.AttentionHeads)
            .HasColumnType("int")
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.DurationMs)
            .HasColumnType("int")
            ;

        builder.Property(e => e.MaxReasoningSteps)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ModelId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ProblemId)
            .HasColumnType("uniqueidentifier")
            ;

        builder.Property(e => e.Query)
            .HasColumnType("nvarchar(max)")
            .IsRequired()
            ;

        builder.Property(e => e.ReasoningSteps)
            .HasColumnType("json")
            ;

        builder.Property(e => e.TotalSteps)
            .HasColumnType("int")
            ;

        builder.HasOne(d => d.Model)
            .WithMany(p => p.AttentionInferenceResults)
            .HasForeignKey(d => new { d.ModelId })
            ;

        builder.HasIndex(e => new { e.CreatedAt })
            .HasDatabaseName("IX_AttentionInferenceResults_CreatedAt")
            ;

        builder.HasIndex(e => new { e.ProblemId })
            .HasDatabaseName("IX_AttentionInferenceResults_ProblemId")
            ;
    }
}

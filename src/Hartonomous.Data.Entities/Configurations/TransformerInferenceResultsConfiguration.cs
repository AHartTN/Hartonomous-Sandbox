using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class TransformerInferenceResultsConfiguration : IEntityTypeConfiguration<TransformerInferenceResults>
{
    public void Configure(EntityTypeBuilder<TransformerInferenceResults> builder)
    {
        builder.ToTable("TransformerInferenceResults", "dbo");
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

        builder.Property(e => e.FeedForwardDim)
            .HasColumnType("int")
            ;

        builder.Property(e => e.InputSequence)
            .HasColumnType("nvarchar(max)")
            .IsRequired()
            ;

        builder.Property(e => e.LayerResults)
            .HasColumnType("json")
            ;

        builder.Property(e => e.Layers)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ModelId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ProblemId)
            .HasColumnType("uniqueidentifier")
            ;

        builder.HasOne(d => d.Model)
            .WithMany(p => p.TransformerInferenceResults)
            .HasForeignKey(d => new { d.ModelId })
            ;

        builder.HasIndex(e => new { e.CreatedAt })
            .HasDatabaseName("IX_TransformerInferenceResults_CreatedAt")
            ;

        builder.HasIndex(e => new { e.ProblemId })
            .HasDatabaseName("IX_TransformerInferenceResults_ProblemId")
            ;
    }
}

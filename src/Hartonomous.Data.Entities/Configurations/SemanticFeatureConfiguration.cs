using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class SemanticFeatureConfiguration : IEntityTypeConfiguration<SemanticFeature>
{
    public void Configure(EntityTypeBuilder<SemanticFeature> builder)
    {
        builder.ToTable("SemanticFeatures", "dbo");
        builder.HasKey(e => new { e.AtomEmbeddingId });

        builder.Property(e => e.AtomEmbeddingId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.AvgWordLength)
            .HasColumnType("float")
            ;

        builder.Property(e => e.ComplexityScore)
            .HasColumnType("float")
            ;

        builder.Property(e => e.ComputedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.FormalityScore)
            .HasColumnType("float")
            ;

        builder.Property(e => e.ReferenceDate)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.SentimentScore)
            .HasColumnType("float")
            ;

        builder.Property(e => e.TemporalRelevance)
            .HasColumnType("float")
            ;

        builder.Property(e => e.TextLength)
            .HasColumnType("int")
            ;

        builder.Property(e => e.TopicBusiness)
            .HasColumnType("float")
            ;

        builder.Property(e => e.TopicCreative)
            .HasColumnType("float")
            ;

        builder.Property(e => e.TopicScientific)
            .HasColumnType("float")
            ;

        builder.Property(e => e.TopicTechnical)
            .HasColumnType("float")
            ;

        builder.Property(e => e.UniqueWordRatio)
            .HasColumnType("float")
            ;

        builder.Property(e => e.WordCount)
            .HasColumnType("int")
            ;

        builder.HasOne(d => d.AtomEmbedding)
            .WithOne(p => p.SemanticFeature)
            .HasForeignKey<SemanticFeature>(e => new { e.AtomEmbeddingId })
            ;

        builder.HasIndex(e => new { e.SentimentScore })
            .HasDatabaseName("ix_semantic_sentiment")
            ;

        builder.HasIndex(e => new { e.TemporalRelevance })
            .HasDatabaseName("ix_semantic_temporal")
            ;

        builder.HasIndex(e => new { e.TopicBusiness })
            .HasDatabaseName("ix_semantic_topic_business")
            ;

        builder.HasIndex(e => new { e.TopicCreative })
            .HasDatabaseName("ix_semantic_topic_creative")
            ;

        builder.HasIndex(e => new { e.TopicScientific })
            .HasDatabaseName("ix_semantic_topic_scientific")
            ;

        builder.HasIndex(e => new { e.TopicTechnical })
            .HasDatabaseName("ix_semantic_topic_technical")
            ;
    }
}

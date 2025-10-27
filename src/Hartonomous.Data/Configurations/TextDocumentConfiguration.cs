using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public sealed class TextDocumentConfiguration : IEntityTypeConfiguration<TextDocument>
{
    public void Configure(EntityTypeBuilder<TextDocument> builder)
    {
        builder.ToTable("TextDocuments", "dbo");

        builder.HasKey(e => e.DocId);
        builder.Property(e => e.DocId).HasColumnName("doc_id").ValueGeneratedOnAdd();

        builder.Property(e => e.SourcePath).HasColumnName("source_path").HasMaxLength(500);
        builder.Property(e => e.SourceUrl).HasColumnName("source_url").HasMaxLength(1000);

        // Text content
        builder.Property(e => e.RawText).HasColumnName("raw_text").IsRequired();
        builder.Property(e => e.Language).HasColumnName("language").HasMaxLength(10);
        builder.Property(e => e.CharCount).HasColumnName("char_count");
        builder.Property(e => e.WordCount).HasColumnName("word_count");

        // Vector representations
        builder.Property(e => e.GlobalEmbedding).HasColumnName("global_embedding").HasColumnType("VECTOR(768)");
        builder.Property(e => e.GlobalEmbeddingDim).HasColumnName("global_embedding_dim");

        // Semantic features
        builder.Property(e => e.TopicVector).HasColumnName("topic_vector").HasColumnType("VECTOR(100)");
        builder.Property(e => e.SentimentScore).HasColumnName("sentiment_score");
        builder.Property(e => e.Toxicity).HasColumnName("toxicity");

        // Metadata
        builder.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("JSON");

        builder.Property(e => e.IngestionDate).HasColumnName("ingestion_date").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.LastAccessed).HasColumnName("last_accessed");
        builder.Property(e => e.AccessCount).HasColumnName("access_count").HasDefaultValue(0L);
    }
}

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
        builder.Property(e => e.DocId).ValueGeneratedOnAdd();

        builder.Property(e => e.SourcePath).HasMaxLength(500);
        builder.Property(e => e.SourceUrl).HasMaxLength(1000);

        // Text content
        builder.Property(e => e.RawText).IsRequired();
        builder.Property(e => e.Language).HasMaxLength(10);
        builder.Property(e => e.CharCount);
        builder.Property(e => e.WordCount);

        // Vector representations
        builder.Property(e => e.GlobalEmbedding).HasColumnType("VECTOR(768)");
        builder.Property(e => e.GlobalEmbeddingDim);

        // Semantic features
        builder.Property(e => e.TopicVector).HasColumnType("VECTOR(100)");
        builder.Property(e => e.SentimentScore);
        builder.Property(e => e.Toxicity);

        // Metadata
        builder.Property(e => e.Metadata).HasColumnType("JSON");

        builder.Property(e => e.IngestionDate).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.LastAccessed);
        builder.Property(e => e.AccessCount).HasDefaultValue(0L);
    }
}

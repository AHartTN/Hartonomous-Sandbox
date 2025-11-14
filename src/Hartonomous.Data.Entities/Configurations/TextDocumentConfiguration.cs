using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class TextDocumentConfiguration : IEntityTypeConfiguration<TextDocument>
{
    public void Configure(EntityTypeBuilder<TextDocument> builder)
    {
        builder.ToTable("TextDocuments", "dbo");
        builder.HasKey(e => new { e.DocId });

        builder.Property(e => e.DocId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.AccessCount)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.CharCount)
            .HasColumnType("int")
            ;

        builder.Property(e => e.GlobalEmbedding)
            .HasColumnType("vector(1998)")
            .HasMaxLength(1998)
            ;

        builder.Property(e => e.IngestionDate)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Language)
            .HasColumnType("nvarchar(10)")
            .HasMaxLength(10)
            ;

        builder.Property(e => e.LastAccessed)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Metadata)
            .HasColumnType("json")
            ;

        builder.Property(e => e.RawText)
            .HasColumnType("nvarchar(max)")
            .IsRequired()
            ;

        builder.Property(e => e.SentimentScore)
            .HasColumnType("real")
            ;

        builder.Property(e => e.SourcePath)
            .HasColumnType("nvarchar(500)")
            .HasMaxLength(500)
            ;

        builder.Property(e => e.SourceUrl)
            .HasColumnType("nvarchar(1000)")
            .HasMaxLength(1000)
            ;

        builder.Property(e => e.TopicVector)
            .HasColumnType("vector(1998)")
            .HasMaxLength(1998)
            ;

        builder.Property(e => e.Toxicity)
            .HasColumnType("real")
            ;

        builder.Property(e => e.WordCount)
            .HasColumnType("int")
            ;
    }
}

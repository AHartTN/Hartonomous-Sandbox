using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Infrastructure.Data.Configurations;

public class TokenVocabularyConfiguration : IEntityTypeConfiguration<TokenVocabulary>
{
    public void Configure(EntityTypeBuilder<TokenVocabulary> builder)
    {
        builder.ToTable("TokenVocabulary");

        builder.HasKey(tv => tv.VocabId);

        builder.Property(tv => tv.Token)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(tv => tv.TokenType)
            .HasMaxLength(20);

        // VECTOR type for token embeddings
        builder.Property(tv => tv.Embedding)
            .HasColumnType("VECTOR(768)");

        builder.Property(tv => tv.Frequency)
            .HasDefaultValue(0);

        // Unique constraint on model_id + token_id
        builder.HasIndex(tv => new { tv.ModelId, tv.TokenId })
            .IsUnique()
            .HasDatabaseName("IX_TokenVocabulary_ModelId_TokenId");

        // Index for token lookup
        builder.HasIndex(tv => new { tv.ModelId, tv.Token })
            .HasDatabaseName("IX_TokenVocabulary_ModelId_Token");

        builder.HasOne(tv => tv.Model)
            .WithMany()
            .HasForeignKey(tv => tv.ModelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public class AtomicTextTokenConfiguration : IEntityTypeConfiguration<AtomicTextToken>
{
    public void Configure(EntityTypeBuilder<AtomicTextToken> builder)
    {
        builder.HasKey(t => t.TokenId);

        builder.Property(t => t.TokenId)
            .ValueGeneratedOnAdd();

        builder.Property(t => t.TokenHash)
            .HasColumnType("BINARY(32)")
            .IsRequired();

        builder.Property(t => t.TokenText)
            .HasColumnType("NVARCHAR(200)")
            .IsRequired();

        builder.Property(t => t.TokenLength)
            .IsRequired();

        builder.Property(t => t.TokenEmbedding)
            .HasColumnType("VECTOR(768)");

        builder.Property(t => t.EmbeddingModel)
            .HasColumnType("NVARCHAR(100)");

        builder.Property(t => t.VocabId)
            .IsRequired(false);

        builder.Property(t => t.ReferenceCount)
            .HasColumnType("BIGINT")
            .HasDefaultValue(0L);

        builder.Property(t => t.FirstSeen)
            .HasColumnType("DATETIME2")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(t => t.LastReferenced)
            .HasColumnType("DATETIME2")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(t => t.TokenHash)
            .IsUnique();

        builder.HasIndex(t => t.TokenText)
            .IsUnique()
            .HasDatabaseName("idx_token_text");
    }
}
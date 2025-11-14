using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AtomicTextTokenConfiguration : IEntityTypeConfiguration<AtomicTextToken>
{
    public void Configure(EntityTypeBuilder<AtomicTextToken> builder)
    {
        builder.ToTable("AtomicTextTokens", "dbo");
        builder.HasKey(e => new { e.TokenId });

        builder.Property(e => e.TokenId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.EmbeddingModel)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            ;

        builder.Property(e => e.FirstSeen)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.LastReferenced)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.ReferenceCount)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.TokenEmbedding)
            .HasColumnType("vector(1998)")
            .HasMaxLength(1998)
            ;

        builder.Property(e => e.TokenHash)
            .HasColumnType("binary(32)")
            .HasMaxLength(32)
            .IsRequired()
            ;

        builder.Property(e => e.TokenLength)
            .HasColumnType("int")
            ;

        builder.Property(e => e.TokenText)
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired()
            ;

        builder.Property(e => e.VocabId)
            .HasColumnType("int")
            ;

        builder.HasIndex(e => new { e.TokenHash })
            .HasDatabaseName("IX_AtomicTextTokens_TokenHash")
            .IsUnique()
            ;

        builder.HasIndex(e => new { e.TokenText })
            .HasDatabaseName("IX_AtomicTextTokens_TokenText")
            .IsUnique()
            ;
    }
}

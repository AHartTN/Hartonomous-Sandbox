using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class TokenVocabularyConfiguration : IEntityTypeConfiguration<TokenVocabulary>
{
    public void Configure(EntityTypeBuilder<TokenVocabulary> builder)
    {
        builder.ToTable("TokenVocabulary", "dbo");
        builder.HasKey(e => new { e.VocabId });

        builder.Property(e => e.VocabId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.Embedding)
            .HasColumnType("vector(1998)")
            .HasMaxLength(1998)
            ;

        builder.Property(e => e.Frequency)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.LastUsed)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.ModelId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.Token)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired()
            ;

        builder.Property(e => e.TokenId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.TokenType)
            .HasColumnType("nvarchar(20)")
            .HasMaxLength(20)
            ;

        builder.HasOne(d => d.Model)
            .WithMany(p => p.TokenVocabulary)
            .HasForeignKey(d => new { d.ModelId })
            ;
    }
}

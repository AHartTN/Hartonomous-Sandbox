using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public class TokenVocabularyConfiguration : IEntityTypeConfiguration<TokenVocabulary>
{
    public void Configure(EntityTypeBuilder<TokenVocabulary> builder)
    {
        builder.ToTable("TokenVocabulary");

        builder.HasKey(t => t.VocabId);

        builder.Property(t => t.VocabId)
            .HasColumnName("TokenId")
            .UseIdentityColumn();

        builder.Property(t => t.VocabularyName)
            .IsRequired()
            .HasMaxLength(128)
            .HasDefaultValue("default");

        builder.Property(t => t.Token)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(t => t.DimensionIndex)
            .IsRequired();

        builder.Property(t => t.Frequency)
            .HasDefaultValue(1L);

        builder.Property(t => t.IDF)
            .HasColumnType("float");

        builder.Property(t => t.CreatedUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(t => t.UpdatedUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(t => new { t.VocabularyName, t.Token })
            .HasDatabaseName("IX_TokenVocabulary_Token");

        builder.HasIndex(t => t.DimensionIndex)
            .HasDatabaseName("IX_TokenVocabulary_Dimension");

        builder.Property(t => t.Embedding)
            .HasColumnType("VECTOR(768)");

        builder.HasIndex(tv => new { tv.ModelId, tv.Token })
            .HasDatabaseName("IX_TokenVocabulary_ModelId_Token");

        builder.HasOne(tv => tv.Model)
            .WithMany()
            .HasForeignKey(tv => tv.ModelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

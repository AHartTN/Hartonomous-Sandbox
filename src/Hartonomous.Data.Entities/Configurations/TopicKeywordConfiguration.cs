using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class TopicKeywordConfiguration : IEntityTypeConfiguration<TopicKeyword>
{
    public void Configure(EntityTypeBuilder<TopicKeyword> builder)
    {
        builder.ToTable("TopicKeywords", "dbo");
        builder.HasKey(e => new { e.KeywordId });

        builder.Property(e => e.KeywordId)
            .HasColumnName("keyword_id")
            .HasColumnType("int")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.Keyword)
            .HasColumnName("keyword")
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            ;

        builder.Property(e => e.TopicName)
            .HasColumnName("topic_name")
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            ;

        builder.Property(e => e.Weight)
            .HasColumnName("weight")
            .HasColumnType("float")
            ;
    }
}

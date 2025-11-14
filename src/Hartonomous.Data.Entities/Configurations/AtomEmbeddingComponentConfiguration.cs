using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AtomEmbeddingComponentConfiguration : IEntityTypeConfiguration<AtomEmbeddingComponent>
{
    public void Configure(EntityTypeBuilder<AtomEmbeddingComponent> builder)
    {
        builder.ToTable("AtomEmbeddingComponents", "dbo");
        builder.HasKey(e => new { e.AtomEmbeddingComponentId });

        builder.Property(e => e.AtomEmbeddingComponentId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.AtomEmbeddingId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.ComponentIndex)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ComponentValue)
            .HasColumnType("real")
            ;

        builder.HasOne(d => d.AtomEmbedding)
            .WithMany(p => p.AtomEmbeddingComponents)
            .HasForeignKey(d => new { d.AtomEmbeddingId })
            ;

        builder.HasIndex(e => new { e.AtomEmbeddingId, e.ComponentIndex })
            .HasDatabaseName("UX_AtomEmbeddingComponents_Embedding_Index")
            .IsUnique()
            ;
    }
}

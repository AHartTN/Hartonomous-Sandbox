using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public class AtomEmbeddingComponentConfiguration : IEntityTypeConfiguration<AtomEmbeddingComponent>
{
    public void Configure(EntityTypeBuilder<AtomEmbeddingComponent> builder)
    {
        builder.ToTable("AtomEmbeddingComponents");

        builder.HasKey(c => c.AtomEmbeddingComponentId);

        builder.Property(c => c.ComponentIndex)
            .IsRequired();

        builder.Property(c => c.ComponentValue)
            .HasColumnType("real");

        builder.HasIndex(c => new { c.AtomEmbeddingId, c.ComponentIndex })
            .IsUnique()
            .HasDatabaseName("UX_AtomEmbeddingComponents_Embedding_Index");

        builder.HasOne(c => c.AtomEmbedding)
            .WithMany(e => e.Components)
            .HasForeignKey(c => c.AtomEmbeddingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

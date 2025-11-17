using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AtomEmbeddingComponentConfiguration : IEntityTypeConfiguration<AtomEmbeddingComponent>
{
    public void Configure(EntityTypeBuilder<AtomEmbeddingComponent> builder)
    {
        builder.ToTable("AtomEmbeddingComponent", "dbo");
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
            .WithMany(p => p.AtomEmbeddingComponent)
            .HasForeignKey(d => new { d.AtomEmbeddingId })
            ;
    }
}

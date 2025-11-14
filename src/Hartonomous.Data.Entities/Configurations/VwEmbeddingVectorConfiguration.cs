using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class VwEmbeddingVectorConfiguration : IEntityTypeConfiguration<VwEmbeddingVector>
{
    public void Configure(EntityTypeBuilder<VwEmbeddingVector> builder)
    {
        builder.ToTable("");

        builder.Property(e => e.AtomRelationId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.ComponentIndex)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ComponentValue)
            .HasColumnType("real")
            ;

        builder.Property(e => e.SourceAtomId)
            .HasColumnType("bigint")
            ;
    }
}

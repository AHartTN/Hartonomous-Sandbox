using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class EmbeddingMigrationProgressConfiguration : IEntityTypeConfiguration<EmbeddingMigrationProgress>
{
    public void Configure(EntityTypeBuilder<EmbeddingMigrationProgress> builder)
    {
        builder.ToTable("EmbeddingMigrationProgress", "dbo");
        builder.HasKey(e => new { e.AtomEmbeddingId });

        builder.Property(e => e.AtomEmbeddingId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.AtomCount)
            .HasColumnType("int")
            ;

        builder.Property(e => e.MigratedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.RelationCount)
            .HasColumnType("int")
            ;
    }
}

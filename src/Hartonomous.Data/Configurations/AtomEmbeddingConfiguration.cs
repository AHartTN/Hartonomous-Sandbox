using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public class AtomEmbeddingConfiguration : IEntityTypeConfiguration<AtomEmbedding>
{
    public void Configure(EntityTypeBuilder<AtomEmbedding> builder)
    {
        builder.ToTable("AtomEmbeddings");

        builder.HasKey(e => e.AtomEmbeddingId);

        builder.Property(e => e.EmbeddingType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.Dimension)
            .HasDefaultValue(0);

        builder.Property(e => e.EmbeddingVector)
            .HasColumnType("VECTOR(1998)");

        builder.Property(e => e.UsesMaxDimensionPadding)
            .HasDefaultValue(false);

        builder.Property(e => e.Metadata)
            .HasColumnType("JSON");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(e => e.SpatialGeometry)
            .HasColumnType("geometry");

        builder.Property(e => e.SpatialCoarse)
            .HasColumnType("geometry");

        builder.HasIndex(e => new { e.AtomId, e.EmbeddingType, e.ModelId })
            .HasDatabaseName("IX_AtomEmbeddings_Atom_Model_Type");

        builder.HasOne(e => e.Atom)
            .WithMany(a => a.Embeddings)
            .HasForeignKey(e => e.AtomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Model)
            .WithMany(m => m.AtomEmbeddings)
            .HasForeignKey(e => e.ModelId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

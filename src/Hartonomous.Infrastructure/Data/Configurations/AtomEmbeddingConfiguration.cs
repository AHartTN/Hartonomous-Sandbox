using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Infrastructure.Data.Configurations;

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

        builder.Property(e => e.SpatialProjX)
            .HasColumnType("float")
            .HasColumnName("SpatialProjX");

        builder.Property(e => e.SpatialProjY)
            .HasColumnType("float")
            .HasColumnName("SpatialProjY");

        builder.Property(e => e.SpatialProjZ)
            .HasColumnType("float")
            .HasColumnName("SpatialProjZ");

        builder.Property(e => e.Metadata)
            .HasColumnType("JSON");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(e => e.SpatialGeometry)
            .HasColumnType("geometry");

        builder.Property(e => e.SpatialCoarse)
            .HasColumnType("geometry");

        builder.Property(e => e.SpatialBucketX)
            .HasColumnType("int")
            .HasColumnName("SpatialBucketX");

        builder.Property(e => e.SpatialBucketY)
            .HasColumnType("int")
            .HasColumnName("SpatialBucketY");

        builder.Property(e => e.SpatialBucketZ)
            .HasColumnType("int")
            .HasColumnName("SpatialBucketZ")
            .IsRequired()
            .HasDefaultValue(int.MinValue);

        builder.HasIndex(e => new { e.AtomId, e.EmbeddingType, e.ModelId })
            .HasDatabaseName("IX_AtomEmbeddings_Atom_Model_Type");

        builder.HasIndex(e => new { e.SpatialBucketX, e.SpatialBucketY, e.SpatialBucketZ })
            .HasDatabaseName("IX_AtomEmbeddings_SpatialBucket");

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

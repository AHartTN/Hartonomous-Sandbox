using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Entities.Configurations;

public class AtomEmbeddingConfiguration : IEntityTypeConfiguration<AtomEmbedding>
{
    public void Configure(EntityTypeBuilder<AtomEmbedding> builder)
    {
        builder.ToTable("AtomEmbedding", "dbo");
        builder.HasKey(e => new { e.AtomEmbeddingId });

        builder.Property(e => e.AtomEmbeddingId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.AtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Dimension)
            .HasColumnType("int")
            ;

        builder.Property(e => e.EmbeddingType)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.Property(e => e.EmbeddingVector)
            .HasColumnType("vector(1998)")
            .HasMaxLength(1998)
            ;

        builder.Property(e => e.HilbertValue)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.ModelId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.SpatialBucketX)
            .HasColumnType("int")
            ;

        builder.Property(e => e.SpatialBucketY)
            .HasColumnType("int")
            ;

        builder.Property(e => e.SpatialBucketZ)
            .HasColumnType("int")
            ;

        builder.Property(e => e.SpatialKey)
            .HasColumnType("geometry")
            .IsRequired()
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            ;

        builder.HasOne(d => d.Atom)
            .WithMany(p => p.AtomEmbeddings)
            .HasForeignKey(d => new { d.AtomId })
            ;

        builder.HasOne(d => d.Model)
            .WithMany(p => p.AtomEmbeddings)
            .HasForeignKey(d => new { d.ModelId })
            ;

        builder.HasIndex(e => new { e.AtomId })
            .HasDatabaseName("IX_AtomEmbedding_Atom")
            ;

        builder.HasIndex(e => new { e.AtomId })
            .HasDatabaseName("IX_AtomEmbedding_AtomId")
            ;

        builder.HasIndex(e => new { e.AtomId, e.ModelId })
            .HasDatabaseName("IX_AtomEmbedding_AtomId_ModelId")
            ;

        builder.HasIndex(e => new { e.CreatedAt })
            .HasDatabaseName("IX_AtomEmbedding_CreatedAt")
            ;

        builder.HasIndex(e => new { e.Dimension, e.EmbeddingType })
            .HasDatabaseName("IX_AtomEmbedding_Dimension")
            ;

        builder.HasIndex(e => new { e.EmbeddingType, e.Dimension, e.ModelId })
            .HasDatabaseName("IX_AtomEmbedding_EmbeddingType_Dimension")
            ;

        builder.HasIndex(e => new { e.HilbertValue })
            .HasDatabaseName("IX_AtomEmbedding_Hilbert")
            ;

        builder.HasIndex(e => new { e.SpatialBucketX, e.SpatialBucketY, e.SpatialBucketZ })
            .HasDatabaseName("IX_AtomEmbedding_SpatialBuckets")
            ;

        builder.HasIndex(e => new { e.TenantId, e.ModelId, e.EmbeddingType })
            .HasDatabaseName("IX_AtomEmbedding_TenantId_ModelId")
            ;

        builder.HasIndex(e => new { e.SpatialKey })
            .HasDatabaseName("SIX_AtomEmbedding_SpatialKey")
            ;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AtomEmbeddingsConfiguration : IEntityTypeConfiguration<AtomEmbeddings>
{
    public void Configure(EntityTypeBuilder<AtomEmbeddings> builder)
    {
        builder.ToTable("AtomEmbeddings", "dbo");
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
            .HasDatabaseName("IX_AtomEmbeddings_Atom")
            ;

        builder.HasIndex(e => new { e.AtomId })
            .HasDatabaseName("IX_AtomEmbeddings_AtomId")
            ;

        builder.HasIndex(e => new { e.Dimension, e.EmbeddingType })
            .HasDatabaseName("IX_AtomEmbeddings_Dimension")
            ;

        builder.HasIndex(e => new { e.HilbertValue })
            .HasDatabaseName("IX_AtomEmbeddings_Hilbert")
            ;

        builder.HasIndex(e => new { e.SpatialBucketX, e.SpatialBucketY, e.SpatialBucketZ })
            .HasDatabaseName("IX_AtomEmbeddings_SpatialBuckets")
            ;

        builder.HasIndex(e => new { e.TenantId, e.ModelId, e.EmbeddingType })
            .HasDatabaseName("IX_AtomEmbeddings_TenantId_ModelId")
            ;

        builder.HasIndex(e => new { e.SpatialKey })
            .HasDatabaseName("SIX_AtomEmbeddings_SpatialKey")
            ;
    }
}

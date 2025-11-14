using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AtomEmbeddingConfiguration : IEntityTypeConfiguration<AtomEmbedding>
{
    public void Configure(EntityTypeBuilder<AtomEmbedding> builder)
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

        builder.Property(e => e.LastAccessedUtc)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.LastComputedUtc)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.LastUpdated)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Metadata)
            .HasColumnType("json")
            ;

        builder.Property(e => e.ModelId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.SpatialBucket)
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

        builder.Property(e => e.SpatialCoarse)
            .HasColumnType("geometry")
            .IsRequired()
            ;

        builder.Property(e => e.SpatialGeometry)
            .HasColumnType("geometry")
            .IsRequired()
            ;

        builder.Property(e => e.SpatialProjX)
            .HasColumnType("float")
            ;

        builder.Property(e => e.SpatialProjY)
            .HasColumnType("float")
            ;

        builder.Property(e => e.SpatialProjZ)
            .HasColumnType("float")
            ;

        builder.Property(e => e.SpatialProjection3D)
            .HasColumnType("geometry")
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
            .OnDelete(DeleteBehavior.ClientSetNull)
            ;

        builder.HasIndex(e => new { e.AtomId })
            .HasDatabaseName("IX_AtomEmbeddings_Atom")
            ;

        builder.HasIndex(e => new { e.AtomId, e.EmbeddingType, e.ModelId })
            .HasDatabaseName("IX_AtomEmbeddings_Atom_Model_Type")
            ;

        builder.HasIndex(e => new { e.SpatialBucket })
            .HasDatabaseName("IX_AtomEmbeddings_Bucket")
            ;

        builder.HasIndex(e => new { e.SpatialBucketX, e.SpatialBucketY, e.SpatialBucketZ })
            .HasDatabaseName("IX_AtomEmbeddings_BucketXYZ")
            ;

        builder.HasIndex(e => new { e.SpatialCoarse })
            .HasDatabaseName("IX_AtomEmbeddings_Coarse")
            ;

        builder.HasIndex(e => new { e.ModelId })
            .HasDatabaseName("IX_AtomEmbeddings_ModelId")
            ;

        builder.HasIndex(e => new { e.SpatialGeometry })
            .HasDatabaseName("IX_AtomEmbeddings_Spatial")
            ;

        builder.HasIndex(e => new { e.SpatialBucketX, e.SpatialBucketY, e.SpatialBucketZ })
            .HasDatabaseName("IX_AtomEmbeddings_SpatialBucket")
            ;

        builder.HasIndex(e => new { e.SpatialCoarse })
            .HasDatabaseName("IX_AtomEmbeddings_SpatialCoarse")
            ;

        builder.HasIndex(e => new { e.SpatialGeometry })
            .HasDatabaseName("IX_AtomEmbeddings_SpatialGeometry")
            ;
    }
}

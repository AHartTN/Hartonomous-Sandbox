using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AtomEmbeddingSpatialMetadatumConfiguration : IEntityTypeConfiguration<AtomEmbeddingSpatialMetadatum>
{
    public void Configure(EntityTypeBuilder<AtomEmbeddingSpatialMetadatum> builder)
    {
        builder.ToTable("AtomEmbeddingSpatialMetadatum", "dbo");
        builder.HasKey(e => new { e.MetadataId });

        builder.Property(e => e.MetadataId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.EmbeddingCount)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.HasZ)
            .HasColumnType("bit")
            ;

        builder.Property(e => e.MaxProjX)
            .HasColumnType("float")
            ;

        builder.Property(e => e.MaxProjY)
            .HasColumnType("float")
            ;

        builder.Property(e => e.MaxProjZ)
            .HasColumnType("float")
            ;

        builder.Property(e => e.MinProjX)
            .HasColumnType("float")
            ;

        builder.Property(e => e.MinProjY)
            .HasColumnType("float")
            ;

        builder.Property(e => e.MinProjZ)
            .HasColumnType("float")
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

        builder.Property(e => e.UpdatedAt)
            .HasColumnType("datetime2")
            ;

        builder.HasIndex(e => new { e.SpatialBucketX, e.SpatialBucketY, e.SpatialBucketZ })
            .HasDatabaseName("IX_AtomEmbeddingSpatialMetadata_BucketXYZ")
            ;

        builder.HasIndex(e => new { e.SpatialBucketX, e.SpatialBucketY, e.SpatialBucketZ, e.HasZ })
            .HasDatabaseName("UX_AtomEmbeddingSpatialMetadatum_BucketXYZ")
            .IsUnique()
            ;
    }
}

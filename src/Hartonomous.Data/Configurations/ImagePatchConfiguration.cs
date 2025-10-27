using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public sealed class ImagePatchConfiguration : IEntityTypeConfiguration<ImagePatch>
{
    public void Configure(EntityTypeBuilder<ImagePatch> builder)
    {
        builder.ToTable("ImagePatches", "dbo");

        builder.HasKey(e => e.PatchId);
        builder.Property(e => e.PatchId).HasColumnName("patch_id").ValueGeneratedOnAdd();

        builder.Property(e => e.ImageId).HasColumnName("image_id").IsRequired();
        builder.Property(e => e.PatchX).HasColumnName("patch_x").IsRequired();
        builder.Property(e => e.PatchY).HasColumnName("patch_y").IsRequired();
        builder.Property(e => e.PatchWidth).HasColumnName("patch_width").IsRequired();
        builder.Property(e => e.PatchHeight).HasColumnName("patch_height").IsRequired();

        // Spatial
        builder.Property(e => e.PatchRegion).HasColumnName("patch_region").HasColumnType("GEOMETRY").IsRequired();

        // Features
        builder.Property(e => e.PatchEmbedding).HasColumnName("patch_embedding").HasColumnType("VECTOR(768)");
        builder.Property(e => e.DominantColor).HasColumnName("dominant_color").HasColumnType("GEOMETRY");
        builder.Property(e => e.TextureFeatures).HasColumnName("texture_features");

        // Statistics
        builder.Property(e => e.MeanIntensity).HasColumnName("mean_intensity");
        builder.Property(e => e.StdIntensity).HasColumnName("std_intensity");

        // Indexes
        builder.HasIndex(e => new { e.ImageId, e.PatchX, e.PatchY }).HasDatabaseName("idx_image_patches");
    }
}

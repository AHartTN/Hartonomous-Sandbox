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
        builder.Property(e => e.PatchId).ValueGeneratedOnAdd();

        builder.Property(e => e.ImageId).IsRequired();
        builder.Property(e => e.PatchX).IsRequired();
        builder.Property(e => e.PatchY).IsRequired();
        builder.Property(e => e.PatchWidth).IsRequired();
        builder.Property(e => e.PatchHeight).IsRequired();

        // Spatial
        builder.Property(e => e.PatchRegion).HasColumnType("GEOMETRY").IsRequired();

        // Features
        builder.Property(e => e.PatchEmbedding).HasColumnType("VECTOR(768)");
        builder.Property(e => e.DominantColor).HasColumnType("GEOMETRY");

        // Statistics
        builder.Property(e => e.MeanIntensity);
        builder.Property(e => e.StdIntensity);

        // Indexes
        builder.HasIndex(e => new { e.ImageId, e.PatchX, e.PatchY })
            .HasDatabaseName("IX_ImagePatches_ImageId_PatchX_PatchY");
    }
}

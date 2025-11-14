using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class ImagePatchConfiguration : IEntityTypeConfiguration<ImagePatch>
{
    public void Configure(EntityTypeBuilder<ImagePatch> builder)
    {
        builder.ToTable("ImagePatches", "dbo");
        builder.HasKey(e => new { e.PatchId });

        builder.Property(e => e.PatchId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.ColIndex)
            .HasColumnType("int")
            ;

        builder.Property(e => e.DominantColor)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.ImageId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.MeanB)
            .HasColumnType("float")
            ;

        builder.Property(e => e.MeanG)
            .HasColumnType("float")
            ;

        builder.Property(e => e.MeanIntensity)
            .HasColumnType("real")
            ;

        builder.Property(e => e.MeanR)
            .HasColumnType("float")
            ;

        builder.Property(e => e.ParentAtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.PatchEmbedding)
            .HasColumnType("vector(1998)")
            .HasMaxLength(1998)
            ;

        builder.Property(e => e.PatchGeometry)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.PatchHeight)
            .HasColumnType("int")
            ;

        builder.Property(e => e.PatchIndex)
            .HasColumnType("int")
            ;

        builder.Property(e => e.PatchRegion)
            .HasColumnType("geometry")
            .IsRequired()
            ;

        builder.Property(e => e.PatchWidth)
            .HasColumnType("int")
            ;

        builder.Property(e => e.PatchX)
            .HasColumnType("int")
            ;

        builder.Property(e => e.PatchY)
            .HasColumnType("int")
            ;

        builder.Property(e => e.RowIndex)
            .HasColumnType("int")
            ;

        builder.Property(e => e.StdIntensity)
            .HasColumnType("real")
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.Variance)
            .HasColumnType("float")
            ;

        builder.HasOne(d => d.Image)
            .WithMany(p => p.ImagePatches)
            .HasForeignKey(d => new { d.ImageId })
            ;

        builder.HasOne(d => d.ParentAtom)
            .WithMany(p => p.ImagePatches)
            .HasForeignKey(d => new { d.ParentAtomId })
            .OnDelete(DeleteBehavior.ClientSetNull)
            ;

        builder.HasIndex(e => new { e.ImageId, e.PatchX, e.PatchY })
            .HasDatabaseName("IX_ImagePatches_ImageId_PatchX_PatchY")
            ;
    }
}

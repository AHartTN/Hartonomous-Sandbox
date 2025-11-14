using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class SpatialLandmarkConfiguration : IEntityTypeConfiguration<SpatialLandmark>
{
    public void Configure(EntityTypeBuilder<SpatialLandmark> builder)
    {
        builder.ToTable("SpatialLandmarks", "dbo");
        builder.HasKey(e => new { e.LandmarkId });

        builder.Property(e => e.LandmarkId)
            .HasColumnType("int")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.CreatedUtc)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Description)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.LandmarkPoint)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.LandmarkVector)
            .HasColumnType("vector(1998)")
            .HasMaxLength(1998)
            ;

        builder.Property(e => e.SelectionMethod)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            ;
    }
}

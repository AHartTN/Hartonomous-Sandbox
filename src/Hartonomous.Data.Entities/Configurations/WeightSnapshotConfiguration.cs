using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class WeightSnapshotConfiguration : IEntityTypeConfiguration<WeightSnapshot>
{
    public void Configure(EntityTypeBuilder<WeightSnapshot> builder)
    {
        builder.ToTable("WeightSnapshot", "dbo");
        builder.HasKey(e => new { e.SnapshotId });

        builder.Property(e => e.SnapshotId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Description)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.ModelId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.SnapshotName)
            .HasColumnType("nvarchar(255)")
            .HasMaxLength(255)
            .IsRequired()
            ;

        builder.Property(e => e.SnapshotTime)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.WeightCount)
            .HasColumnType("int")
            ;

        builder.HasOne(d => d.Model)
            .WithMany(p => p.WeightSnapshot)
            .HasForeignKey(d => new { d.ModelId })
            .OnDelete(DeleteBehavior.ClientSetNull)
            ;

        builder.HasIndex(e => new { e.SnapshotName })
            .HasDatabaseName("UQ__WeightSn__FAC0EC4A338BFCE3")
            .IsUnique()
            ;
    }
}

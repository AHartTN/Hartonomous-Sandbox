using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class ModelVersionHistoryConfiguration : IEntityTypeConfiguration<ModelVersionHistory>
{
    public void Configure(EntityTypeBuilder<ModelVersionHistory> builder)
    {
        builder.ToTable("ModelVersionHistory", "provenance");
        builder.HasKey(e => new { e.VersionHistoryId });

        builder.Property(e => e.VersionHistoryId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.ChangeDescription)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.CreatedBy)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            ;

        builder.Property(e => e.ModelId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ParentVersionId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.PerformanceMetrics)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.VersionHash)
            .HasColumnType("nvarchar(64)")
            .HasMaxLength(64)
            ;

        builder.Property(e => e.VersionTag)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.HasOne(d => d.Model)
            .WithMany(p => p.ModelVersionHistory)
            .HasForeignKey(d => new { d.ModelId })
            ;

        builder.HasOne(d => d.ParentVersion)
            .WithMany(p => p.InverseParentVersion)
            .HasForeignKey(d => new { d.ParentVersionId })
            .OnDelete(DeleteBehavior.ClientSetNull)
            ;

        builder.HasIndex(e => new { e.ModelId, e.CreatedAt })
            .HasDatabaseName("IX_ModelVersionHistory_ModelId_CreatedAt")
            ;

        builder.HasIndex(e => new { e.ModelId, e.VersionTag })
            .HasDatabaseName("UX_ModelVersionHistory_ModelId_VersionTag")
            .IsUnique()
            ;
    }
}

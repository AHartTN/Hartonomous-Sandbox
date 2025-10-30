using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public class WeightCatalogConfiguration : IEntityTypeConfiguration<WeightCatalog>
{
    public void Configure(EntityTypeBuilder<WeightCatalog> builder)
    {
        builder.ToTable("WeightCatalog", "dbo");

        builder.HasKey(wc => wc.CatalogId);

        builder.Property(wc => wc.CatalogId)
            .HasColumnName("catalog_id")
            .ValueGeneratedOnAdd();

        builder.Property(wc => wc.WeightId)
            .HasColumnName("weight_id")
            .IsRequired();

        builder.Property(wc => wc.ModelId)
            .HasColumnName("model_id")
            .IsRequired();

        builder.Property(wc => wc.LayerIdx)
            .HasColumnName("layer_idx")
            .IsRequired();

        builder.Property(wc => wc.ComponentType)
            .HasColumnName("component_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(wc => wc.PositionMetadata)
            .HasColumnName("position_metadata")
            .HasColumnType("nvarchar(max)");

        builder.Property(wc => wc.ImportanceScore)
            .HasColumnName("importance_score")
            .HasColumnType("float");

        builder.Property(wc => wc.ContentHash)
            .HasColumnName("content_hash")
            .HasColumnType("binary(32)")
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(wc => wc.CreatedDate)
            .HasColumnName("created_date")
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSDATETIME()");

        // Indexes
        builder.HasIndex(wc => new { wc.ModelId, wc.LayerIdx })
            .IncludeProperties(wc => new
            {
                wc.WeightId,
                wc.ComponentType,
                wc.ImportanceScore
            })
            .HasDatabaseName("IX_WeightCatalog_Model");

        builder.HasIndex(wc => new { wc.ContentHash, wc.ModelId })
            .HasDatabaseName("IX_WeightCatalog_Hash");

        builder.HasIndex(wc => wc.ImportanceScore)
            .HasDatabaseName("IX_WeightCatalog_Importance")
            .HasFilter("[importance_score] IS NOT NULL");

        // Foreign key
        builder.HasOne(wc => wc.Model)
            .WithMany(m => m.Weights)
            .HasForeignKey(wc => wc.ModelId)
            .HasConstraintName("FK_WeightCatalog_Model")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

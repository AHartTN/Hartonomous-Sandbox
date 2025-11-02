using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

/// <summary>
/// EF Core configuration for Model entity
/// </summary>
public class ModelConfiguration : IEntityTypeConfiguration<Model>
{
    public void Configure(EntityTypeBuilder<Model> builder)
    {
        builder.ToTable("Models");

        builder.HasKey(m => m.ModelId);

        builder.Property(m => m.ModelName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.ModelType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Architecture)
            .HasMaxLength(100);

        // SQL Server 2025 native JSON type
        builder.Property(m => m.Config)
            .HasColumnType("JSON");

        builder.Property(m => m.IngestionDate)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(m => m.UsageCount)
            .HasDefaultValue(0);

        // Indexes
        builder.HasIndex(m => m.ModelName)
            .HasDatabaseName("IX_Models_ModelName");

        builder.HasIndex(m => m.ModelType)
            .HasDatabaseName("IX_Models_ModelType");

        // Relationships
        builder.HasMany(m => m.Layers)
            .WithOne(l => l.Model)
            .HasForeignKey(l => l.ModelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Metadata)
            .WithOne(md => md.Model)
            .HasForeignKey<ModelMetadata>(md => md.ModelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

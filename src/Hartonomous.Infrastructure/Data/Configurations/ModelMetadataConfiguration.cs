using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Infrastructure.Data.Configurations;

public class ModelMetadataConfiguration : IEntityTypeConfiguration<ModelMetadata>
{
    public void Configure(EntityTypeBuilder<ModelMetadata> builder)
    {
        builder.ToTable("ModelMetadata");

        builder.HasKey(md => md.MetadataId);

        // SQL Server 2025 native JSON type for all JSON fields
        builder.Property(md => md.SupportedTasks)
            .HasColumnType("JSON");

        builder.Property(md => md.SupportedModalities)
            .HasColumnType("JSON");

        builder.Property(md => md.PerformanceMetrics)
            .HasColumnType("JSON");

        builder.Property(md => md.TrainingDataset)
            .HasMaxLength(500);

        builder.Property(md => md.License)
            .HasMaxLength(100);

        builder.Property(md => md.SourceUrl)
            .HasMaxLength(500);

        // Unique constraint - one metadata per model
        builder.HasIndex(md => md.ModelId)
            .IsUnique();
    }
}

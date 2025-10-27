using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public class InferenceRequestConfiguration : IEntityTypeConfiguration<InferenceRequest>
{
    public void Configure(EntityTypeBuilder<InferenceRequest> builder)
    {
        builder.ToTable("InferenceRequests");

        builder.HasKey(ir => ir.InferenceId);

        builder.Property(ir => ir.RequestTimestamp)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(ir => ir.TaskType)
            .HasMaxLength(50);

        // JSON types for structured data (SQL Server 2025 native JSON)
        builder.Property(ir => ir.InputData)
            .HasColumnType("JSON");

        builder.Property(ir => ir.InputHash)
            .HasMaxLength(32)
            .HasColumnType("binary(32)");

        builder.Property(ir => ir.ModelsUsed)
            .HasColumnType("JSON");  // JSON array of model IDs

        builder.Property(ir => ir.EnsembleStrategy)
            .HasMaxLength(50);

        builder.Property(ir => ir.OutputData)
            .HasColumnType("JSON");

        builder.Property(ir => ir.OutputMetadata)
            .HasColumnType("JSON");

        builder.Property(ir => ir.CacheHit)
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(ir => ir.RequestTimestamp)
            .HasDatabaseName("idx_timestamp")
            .IsDescending();

        builder.HasIndex(ir => ir.TaskType)
            .HasDatabaseName("idx_task_type");

        builder.HasIndex(ir => ir.InputHash)
            .HasDatabaseName("idx_input_hash");

        builder.HasIndex(ir => ir.CacheHit)
            .HasDatabaseName("idx_cache_hit");

        // Relationships
        builder.HasMany(ir => ir.Steps)
            .WithOne(s => s.InferenceRequest)
            .HasForeignKey(s => s.InferenceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

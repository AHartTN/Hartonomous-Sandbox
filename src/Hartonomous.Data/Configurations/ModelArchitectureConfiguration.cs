using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public class ModelArchitectureConfiguration : IEntityTypeConfiguration<ModelArchitecture>
{
    public void Configure(EntityTypeBuilder<ModelArchitecture> builder)
    {
        builder.ToTable("ModelArchitecture", "dbo");

        builder.HasKey(m => m.ModelId);

        builder.Property(m => m.ModelId)
            .HasColumnName("model_id")
            .ValueGeneratedOnAdd();

        builder.Property(m => m.ModelName)
            .HasColumnName("model_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(m => m.ModelType)
            .HasColumnName("model_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.EmbeddingDimension)
            .HasColumnName("embedding_dimension")
            .IsRequired();

        builder.Property(m => m.WeightsTableName)
            .HasColumnName("weights_table_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.LayerCount)
            .HasColumnName("layer_count")
            .IsRequired();

        builder.Property(m => m.ParameterCount)
            .HasColumnName("parameter_count");

        builder.Property(m => m.ArchitectureConfig)
            .HasColumnName("architecture_config")
            .HasColumnType("nvarchar(max)");

        builder.Property(m => m.CreatedDate)
            .HasColumnName("created_date")
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSDATETIME()");

        builder.Property(m => m.LastModifiedDate)
            .HasColumnName("last_modified_date")
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSDATETIME()");

        builder.Property(m => m.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(m => m.ModelName)
            .IsUnique()
            .HasDatabaseName("UQ_ModelName");

        builder.HasIndex(m => m.EmbeddingDimension)
            .HasDatabaseName("IX_ModelArchitecture_Dimension");

        builder.HasIndex(m => m.WeightsTableName)
            .IncludeProperties(m => new { m.ModelId, m.EmbeddingDimension })
            .HasDatabaseName("IX_ModelArchitecture_Table");

        // Check constraints
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_EmbeddingDimension",
                "[embedding_dimension] IN (768, 1536, 1998, 3996)");

            t.HasCheckConstraint("CK_WeightsTable",
                "[weights_table_name] IN ('Weights_768', 'Weights_1536', 'Weights_1998', 'Weights_3996')");
        });

        // Navigation
        builder.HasMany(m => m.Weights)
            .WithOne(w => w.Model)
            .HasForeignKey(w => w.ModelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

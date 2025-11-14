using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class ModelMetadatumConfiguration : IEntityTypeConfiguration<ModelMetadatum>
{
    public void Configure(EntityTypeBuilder<ModelMetadatum> builder)
    {
        builder.ToTable("ModelMetadata", "dbo");
        builder.HasKey(e => new { e.MetadataId });

        builder.Property(e => e.MetadataId)
            .HasColumnType("int")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.EmbeddingDimension)
            .HasColumnType("int")
            ;

        builder.Property(e => e.License)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            ;

        builder.Property(e => e.MaxInputLength)
            .HasColumnType("int")
            ;

        builder.Property(e => e.MaxOutputLength)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ModelId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.PerformanceMetrics)
            .HasColumnType("json")
            ;

        builder.Property(e => e.SourceUrl)
            .HasColumnType("nvarchar(500)")
            .HasMaxLength(500)
            ;

        builder.Property(e => e.SupportedModalities)
            .HasColumnType("json")
            ;

        builder.Property(e => e.SupportedTasks)
            .HasColumnType("json")
            ;

        builder.Property(e => e.TrainingDataset)
            .HasColumnType("nvarchar(500)")
            .HasMaxLength(500)
            ;

        builder.Property(e => e.TrainingDate)
            .HasColumnType("date")
            ;

        builder.HasOne(d => d.Model)
            .WithOne(p => p.ModelMetadatum)
            .HasForeignKey<ModelMetadatum>(e => new { e.ModelId })
            ;

        builder.HasIndex(e => new { e.ModelId })
            .HasDatabaseName("IX_ModelMetadata_ModelId")
            .IsUnique()
            ;
    }
}

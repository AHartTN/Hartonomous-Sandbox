using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class ModelConfiguration : IEntityTypeConfiguration<Model>
{
    public void Configure(EntityTypeBuilder<Model> builder)
    {
        builder.ToTable("Models", "dbo");
        builder.HasKey(e => new { e.ModelId });

        builder.Property(e => e.ModelId)
            .HasColumnType("int")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.Architecture)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            ;

        builder.Property(e => e.AverageInferenceMs)
            .HasColumnType("float")
            ;

        builder.Property(e => e.Config)
            .HasColumnType("json")
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.IngestionDate)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.IsActive)
            .HasColumnType("bit")
            ;

        builder.Property(e => e.LastUsed)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.MetadataJson)
            .HasColumnType("json")
            ;

        builder.Property(e => e.ModelName)
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired()
            ;

        builder.Property(e => e.ModelType)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired()
            ;

        builder.Property(e => e.ModelVersion)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            ;

        builder.Property(e => e.ParameterCount)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.SerializedModel)
            .HasColumnType("varbinary(max)")
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.UsageCount)
            .HasColumnType("bigint")
            ;

        builder.HasIndex(e => new { e.ModelName })
            .HasDatabaseName("IX_Models_ModelName")
            ;

        builder.HasIndex(e => new { e.ModelType })
            .HasDatabaseName("IX_Models_ModelType")
            ;
    }
}

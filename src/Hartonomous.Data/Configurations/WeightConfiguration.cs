using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

/// <summary>
/// Base configuration for all weight tables.
/// </summary>
public abstract class WeightConfigurationBase<TWeight> : IEntityTypeConfiguration<TWeight>
    where TWeight : WeightBase
{
    protected abstract string TableName { get; }
    protected abstract string VectorColumnType { get; }

    public virtual void Configure(EntityTypeBuilder<TWeight> builder)
    {
        builder.ToTable(TableName, "dbo");

        builder.HasKey(w => w.WeightId);

        builder.Property(w => w.WeightId)
            .HasColumnName("weight_id")
            .ValueGeneratedOnAdd();

        builder.Property(w => w.ModelId)
            .HasColumnName("model_id")
            .IsRequired();

        builder.Property(w => w.LayerIdx)
            .HasColumnName("layer_idx")
            .IsRequired();

        builder.Property(w => w.ComponentType)
            .HasColumnName("component_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(w => w.HeadIdx)
            .HasColumnName("head_idx");

        builder.Property(w => w.FromPosition)
            .HasColumnName("from_position");

        builder.Property(w => w.ToPosition)
            .HasColumnName("to_position");

        builder.Property(w => w.WeightVectorJson)
            .HasColumnName("weight_vector")
            .HasColumnType(VectorColumnType)
            .IsRequired();

        builder.Property(w => w.ImportanceScore)
            .HasColumnName("importance_score")
            .HasColumnType("float");

        builder.Property(w => w.LastUpdated)
            .HasColumnName("last_updated")
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSDATETIME()");

        // Ignore computed property
        builder.Ignore(w => w.Dimension);

        // Covering index for model + layer queries
        builder.HasIndex(w => new { w.ModelId, w.LayerIdx })
            .IncludeProperties(w => new
            {
                w.ComponentType,
                w.HeadIdx,
                w.WeightVectorJson,
                w.ImportanceScore
            })
            .HasDatabaseName($"IX_{TableName}_ModelLayer");

        // Index for importance-based queries
        builder.HasIndex(w => new { w.ModelId, w.ImportanceScore })
            .IncludeProperties(w => new { w.LayerIdx, w.WeightVectorJson })
            .HasDatabaseName($"IX_{TableName}_Importance")
            .HasFilter("[importance_score] IS NOT NULL");

        // Foreign key to ModelArchitecture
        builder.HasOne(w => w.Model)
            .WithMany()
            .HasForeignKey(w => w.ModelId)
            .HasConstraintName($"FK_{TableName}_Model")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class Weight768Configuration : WeightConfigurationBase<Weight768>
{
    protected override string TableName => "Weights_768";
    protected override string VectorColumnType => "VECTOR(768)";
}

public class Weight1536Configuration : WeightConfigurationBase<Weight1536>
{
    protected override string TableName => "Weights_1536";
    protected override string VectorColumnType => "VECTOR(1536)";
}

public class Weight1998Configuration : WeightConfigurationBase<Weight1998>
{
    protected override string TableName => "Weights_1998";
    protected override string VectorColumnType => "VECTOR(1998)";
}

public class Weight3996Configuration : WeightConfigurationBase<Weight3996>
{
    protected override string TableName => "Weights_3996";
    protected override string VectorColumnType => "VECTOR(3996, float16)";
}

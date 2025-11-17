using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Entities.Configurations;

public class InferenceRequestConfiguration : IEntityTypeConfiguration<InferenceRequest>
{
    public void Configure(EntityTypeBuilder<InferenceRequest> builder)
    {
        builder.ToTable("InferenceRequest", "dbo");
        builder.HasKey(e => new { e.InferenceId });

        builder.Property(e => e.InferenceId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.CacheHit)
            .HasColumnType("bit")
            ;

        builder.Property(e => e.CompletionTimestamp)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Complexity)
            .HasColumnType("int")
            ;

        builder.Property(e => e.Confidence)
            .HasColumnType("float")
            ;

        builder.Property(e => e.CorrelationId)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.EnsembleStrategy)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            ;

        builder.Property(e => e.EstimatedResponseTimeMs)
            .HasColumnType("int")
            ;

        builder.Property(e => e.InputData)
            .HasColumnType("json")
            ;

        builder.Property(e => e.InputHash)
            .HasColumnType("binary(32)")
            .HasMaxLength(32)
            ;

        builder.Property(e => e.ModelId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ModelsUsed)
            .HasColumnType("json")
            ;

        builder.Property(e => e.OutputData)
            .HasColumnType("json")
            ;

        builder.Property(e => e.OutputMetadata)
            .HasColumnType("json")
            ;

        builder.Property(e => e.RequestTimestamp)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.SlaTier)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            ;

        builder.Property(e => e.Status)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.TaskType)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            ;

        builder.Property(e => e.TotalDurationMs)
            .HasColumnType("int")
            ;

        builder.Property(e => e.UserFeedback)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.UserRating)
            .HasColumnType("tinyint")
            ;

        builder.HasOne(d => d.Model)
            .WithMany(p => p.InferenceRequests)
            .HasForeignKey(d => new { d.ModelId })
            .OnDelete(DeleteBehavior.ClientSetNull)
            ;
    }
}

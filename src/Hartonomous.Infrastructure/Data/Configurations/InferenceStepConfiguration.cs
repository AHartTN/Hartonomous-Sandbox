using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Infrastructure.Data.Configurations;

public class InferenceStepConfiguration : IEntityTypeConfiguration<InferenceStep>
{
    public void Configure(EntityTypeBuilder<InferenceStep> builder)
    {
        builder.ToTable("InferenceSteps");

        builder.HasKey(s => s.StepId);

        builder.Property(s => s.OperationType)
            .HasMaxLength(50);

        builder.Property(s => s.QueryText)
            .HasColumnType("nvarchar(max)");

        builder.Property(s => s.IndexUsed)
            .HasMaxLength(200);

        builder.Property(s => s.CacheUsed)
            .HasDefaultValue(false);

        // Index for querying steps by inference
        builder.HasIndex(s => new { s.InferenceId, s.StepNumber })
            .HasDatabaseName("IX_InferenceSteps_InferenceId_StepNumber");

        // Relationship to Model (optional)
        builder.HasOne(s => s.Model)
            .WithMany()
            .HasForeignKey(s => s.ModelId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

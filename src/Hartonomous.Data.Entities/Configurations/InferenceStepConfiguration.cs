using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class InferenceStepConfiguration : IEntityTypeConfiguration<InferenceStep>
{
    public void Configure(EntityTypeBuilder<InferenceStep> builder)
    {
        builder.ToTable("InferenceStep", "dbo");
        builder.HasKey(e => new { e.StepId });

        builder.Property(e => e.StepId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.CacheUsed)
            .HasColumnType("bit")
            ;

        builder.Property(e => e.DurationMs)
            .HasColumnType("int")
            ;

        builder.Property(e => e.IndexUsed)
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            ;

        builder.Property(e => e.InferenceId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.LayerId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.ModelId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.OperationType)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            ;

        builder.Property(e => e.QueryText)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.RowsExamined)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.RowsReturned)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.StepNumber)
            .HasColumnType("int")
            ;

        builder.HasOne(d => d.Inference)
            .WithMany(p => p.InferenceStep)
            .HasForeignKey(d => new { d.InferenceId })
            ;

        builder.HasOne(d => d.Model)
            .WithMany(p => p.InferenceStep)
            .HasForeignKey(d => new { d.ModelId })
            .OnDelete(DeleteBehavior.ClientSetNull)
            ;
    }
}

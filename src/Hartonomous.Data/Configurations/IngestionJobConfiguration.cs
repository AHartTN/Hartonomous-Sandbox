using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public class IngestionJobConfiguration : IEntityTypeConfiguration<IngestionJob>
{
    public void Configure(EntityTypeBuilder<IngestionJob> builder)
    {
        builder.ToTable("IngestionJobs");

        builder.HasKey(j => j.IngestionJobId);

        builder.Property(j => j.PipelineName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(j => j.Status)
            .HasMaxLength(64);

        builder.Property(j => j.SourceUri)
            .HasMaxLength(1024);

        builder.Property(j => j.Metadata)
            .HasColumnType("JSON");

        builder.Property(j => j.StartedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasMany(j => j.JobAtoms)
            .WithOne(ja => ja.IngestionJob)
            .HasForeignKey(ja => ja.IngestionJobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

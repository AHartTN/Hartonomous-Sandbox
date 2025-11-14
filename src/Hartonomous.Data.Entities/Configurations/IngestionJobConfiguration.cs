using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class IngestionJobConfiguration : IEntityTypeConfiguration<IngestionJob>
{
    public void Configure(EntityTypeBuilder<IngestionJob> builder)
    {
        builder.ToTable("IngestionJobs", "dbo");
        builder.HasKey(e => new { e.IngestionJobId });

        builder.Property(e => e.IngestionJobId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.CompletedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Metadata)
            .HasColumnType("json")
            ;

        builder.Property(e => e.PipelineName)
            .HasColumnType("nvarchar(256)")
            .HasMaxLength(256)
            .IsRequired()
            ;

        builder.Property(e => e.SourceUri)
            .HasColumnType("nvarchar(1024)")
            .HasMaxLength(1024)
            ;

        builder.Property(e => e.StartedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Status)
            .HasColumnType("nvarchar(64)")
            .HasMaxLength(64)
            ;
    }
}

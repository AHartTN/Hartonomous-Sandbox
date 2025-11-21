using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Entities.Configurations;

public class IngestionJobConfiguration : IEntityTypeConfiguration<IngestionJob>
{
    public void Configure(EntityTypeBuilder<IngestionJob> builder)
    {
        builder.ToTable("IngestionJob", "dbo");
        builder.HasKey(e => new { e.IngestionJobId });

        builder.Property(e => e.IngestionJobId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.AtomChunkSize)
            .HasColumnType("int")
            ;

        builder.Property(e => e.AtomQuota)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.CurrentAtomOffset)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.ErrorMessage)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.JobStatus)
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.Property(e => e.LastUpdatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.ModelId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ParentAtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.TotalAtomsProcessed)
            .HasColumnType("bigint")
            ;

        builder.HasOne(d => d.ParentAtom)
            .WithMany(p => p.IngestionJobs)
            .HasForeignKey(d => new { d.ParentAtomId })
            ;

        builder.HasIndex(e => new { e.LastUpdatedAt })
            .HasDatabaseName("IX_IngestionJob_LastUpdatedAt")
            ;

        builder.HasIndex(e => new { e.ParentAtomId, e.JobStatus })
            .HasDatabaseName("IX_IngestionJob_ParentAtomId")
            ;

        builder.HasIndex(e => new { e.JobStatus, e.TenantId })
            .HasDatabaseName("IX_IngestionJobs_Status")
            ;

        builder.HasIndex(e => new { e.TenantId, e.CreatedAt })
            .HasDatabaseName("IX_IngestionJobs_TenantId")
            ;
    }
}

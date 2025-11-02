using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public class AtomicAudioSampleConfiguration : IEntityTypeConfiguration<AtomicAudioSample>
{
    public void Configure(EntityTypeBuilder<AtomicAudioSample> builder)
    {
        builder.HasKey(s => s.SampleHash);

        builder.Property(s => s.SampleHash)
            .HasColumnType("BINARY(32)")
            .IsRequired();

        builder.Property(s => s.AmplitudeNormalized)
            .HasColumnType("FLOAT")
            .IsRequired();

        builder.Property(s => s.AmplitudeInt16)
            .HasColumnType("SMALLINT")
            .IsRequired();

        builder.Property(s => s.ReferenceCount)
            .HasColumnType("BIGINT")
            .HasDefaultValue(0L);

        builder.Property(s => s.FirstSeen)
            .HasColumnType("DATETIME2")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(s => s.LastReferenced)
            .HasColumnType("DATETIME2")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(s => s.AmplitudeNormalized)
            .HasDatabaseName("idx_amplitude");
    }
}

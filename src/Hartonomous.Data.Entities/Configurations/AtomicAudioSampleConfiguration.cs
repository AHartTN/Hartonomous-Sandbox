using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AtomicAudioSampleConfiguration : IEntityTypeConfiguration<AtomicAudioSample>
{
    public void Configure(EntityTypeBuilder<AtomicAudioSample> builder)
    {
        builder.ToTable("AtomicAudioSamples", "dbo");
        builder.HasKey(e => new { e.SampleHash });

        builder.Property(e => e.SampleHash)
            .HasColumnType("binary(32)")
            .HasMaxLength(32)
            .IsRequired()
            ;

        builder.Property(e => e.AmplitudeInt16)
            .HasColumnType("smallint")
            ;

        builder.Property(e => e.AmplitudeNormalized)
            .HasColumnType("real")
            ;

        builder.Property(e => e.AmplitudePoint)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.FirstSeen)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.LastReferenced)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.ReferenceCount)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.SampleBytes)
            .HasColumnType("varbinary(2)")
            .HasMaxLength(2)
            .IsRequired()
            ;

        builder.HasIndex(e => new { e.AmplitudeInt16 })
            .HasDatabaseName("IX_AtomicAudioSamples_Amplitude")
            ;

        builder.HasIndex(e => new { e.AmplitudeNormalized })
            .HasDatabaseName("IX_AtomicAudioSamples_AmplitudeNormalized")
            ;

        builder.HasIndex(e => new { e.ReferenceCount })
            .HasDatabaseName("IX_AtomicAudioSamples_References")
            ;

        builder.HasIndex(e => new { e.AmplitudePoint })
            .HasDatabaseName("SIDX_AtomicAudioSamples_Amplitude")
            ;
    }
}

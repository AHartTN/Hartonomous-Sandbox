using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public sealed class AudioFrameConfiguration : IEntityTypeConfiguration<AudioFrame>
{
    public void Configure(EntityTypeBuilder<AudioFrame> builder)
    {
        builder.ToTable("AudioFrames", "dbo");

        builder.HasKey(e => new { e.AudioId, e.FrameNumber });
        builder.Property(e => e.AudioId).HasColumnName("audio_id").IsRequired();
        builder.Property(e => e.FrameNumber).HasColumnName("frame_number").IsRequired();
        builder.Property(e => e.TimestampMs).HasColumnName("timestamp_ms").IsRequired();

        // Amplitude data
        builder.Property(e => e.AmplitudeL).HasColumnName("amplitude_l");
        builder.Property(e => e.AmplitudeR).HasColumnName("amplitude_r");

        // Spectral features
        builder.Property(e => e.SpectralCentroid).HasColumnName("spectral_centroid");
        builder.Property(e => e.SpectralRolloff).HasColumnName("spectral_rolloff");
        builder.Property(e => e.ZeroCrossingRate).HasColumnName("zero_crossing_rate");
        builder.Property(e => e.RmsEnergy).HasColumnName("rms_energy");

        // MFCC
        builder.Property(e => e.Mfcc).HasColumnName("mfcc").HasColumnType("VECTOR(13)");

        // Frame embedding
        builder.Property(e => e.FrameEmbedding).HasColumnName("frame_embedding").HasColumnType("VECTOR(768)");
    }
}

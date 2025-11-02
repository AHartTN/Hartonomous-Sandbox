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
        builder.Property(e => e.AudioId).IsRequired();
        builder.Property(e => e.FrameNumber).IsRequired();
        builder.Property(e => e.TimestampMs).IsRequired();

        // Amplitude data
        builder.Property(e => e.AmplitudeL);
        builder.Property(e => e.AmplitudeR);

        // Spectral features
        builder.Property(e => e.SpectralCentroid);
        builder.Property(e => e.SpectralRolloff);
        builder.Property(e => e.ZeroCrossingRate);
        builder.Property(e => e.RmsEnergy);

        // MFCC
        builder.Property(e => e.Mfcc).HasColumnType("VECTOR(13)");

        // Frame embedding
        builder.Property(e => e.FrameEmbedding).HasColumnType("VECTOR(768)");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AudioFrameConfiguration : IEntityTypeConfiguration<AudioFrame>
{
    public void Configure(EntityTypeBuilder<AudioFrame> builder)
    {
        builder.ToTable("AudioFrames", "dbo");
        builder.HasKey(e => new { e.AudioId, e.FrameNumber });

        builder.Property(e => e.AudioId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.FrameNumber)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.AmplitudeL)
            .HasColumnType("real")
            ;

        builder.Property(e => e.AmplitudeR)
            .HasColumnType("real")
            ;

        builder.Property(e => e.EndTimeSec)
            .HasColumnType("float")
            ;

        builder.Property(e => e.FrameEmbedding)
            .HasColumnType("vector(1998)")
            .HasMaxLength(1998)
            ;

        builder.Property(e => e.FrameIndex)
            .HasColumnType("int")
            ;

        builder.Property(e => e.Mfcc)
            .HasColumnType("varbinary(max)")
            ;

        builder.Property(e => e.ParentAtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.PeakAmplitude)
            .HasColumnType("float")
            ;

        builder.Property(e => e.RmsAmplitude)
            .HasColumnType("float")
            ;

        builder.Property(e => e.RmsEnergy)
            .HasColumnType("real")
            ;

        builder.Property(e => e.SpectralCentroid)
            .HasColumnType("real")
            ;

        builder.Property(e => e.SpectralRolloff)
            .HasColumnType("real")
            ;

        builder.Property(e => e.StartTimeSec)
            .HasColumnType("float")
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.TimestampMs)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.WaveformGeometry)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.ZeroCrossingRate)
            .HasColumnType("real")
            ;

        builder.HasOne(d => d.Audio)
            .WithMany(p => p.AudioFrames)
            .HasForeignKey(d => new { d.AudioId })
            ;

        builder.HasOne(d => d.ParentAtom)
            .WithMany(p => p.AudioFrames)
            .HasForeignKey(d => new { d.ParentAtomId })
            .OnDelete(DeleteBehavior.ClientSetNull)
            ;
    }
}

using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public sealed class AudioDataConfiguration : IEntityTypeConfiguration<AudioData>
{
    public void Configure(EntityTypeBuilder<AudioData> builder)
    {
        builder.ToTable("AudioData", "dbo");

        builder.HasKey(e => e.AudioId);
        builder.Property(e => e.AudioId).ValueGeneratedOnAdd();

        builder.Property(e => e.SourcePath).HasMaxLength(500);
        builder.Property(e => e.SampleRate).IsRequired();
        builder.Property(e => e.DurationMs).IsRequired();
        builder.Property(e => e.NumChannels).IsRequired();
        builder.Property(e => e.Format).HasMaxLength(20);

        // Spectral representations
        builder.Property(e => e.Spectrogram).HasColumnType("GEOMETRY");
        builder.Property(e => e.MelSpectrogram).HasColumnType("GEOMETRY");

        // Waveforms
        builder.Property(e => e.WaveformLeft).HasColumnType("GEOMETRY");
        builder.Property(e => e.WaveformRight).HasColumnType("GEOMETRY");

        // Vector representations
        builder.Property(e => e.GlobalEmbedding).HasColumnType("VECTOR(768)");
        builder.Property(e => e.GlobalEmbeddingDim);

        // Metadata
        builder.Property(e => e.Metadata).HasColumnType("JSON");

        builder.Property(e => e.IngestionDate).HasDefaultValueSql("SYSUTCDATETIME()");

        // Indexes
        builder.HasIndex(e => e.DurationMs)
            .HasDatabaseName("IX_AudioData_DurationMs");

        builder.HasIndex(e => e.IngestionDate)
            .HasDatabaseName("IX_AudioData_IngestionDate")
            .IsDescending();

        // Relationships
        builder.HasMany(e => e.Frames)
            .WithOne(f => f.Audio)
            .HasForeignKey(f => f.AudioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

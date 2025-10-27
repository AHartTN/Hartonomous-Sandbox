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
        builder.Property(e => e.AudioId).HasColumnName("audio_id").ValueGeneratedOnAdd();

        builder.Property(e => e.SourcePath).HasColumnName("source_path").HasMaxLength(500);

        // Raw data
        builder.Property(e => e.RawData).HasColumnName("raw_data");
        builder.Property(e => e.SampleRate).HasColumnName("sample_rate").IsRequired();
        builder.Property(e => e.DurationMs).HasColumnName("duration_ms").IsRequired();
        builder.Property(e => e.NumChannels).HasColumnName("num_channels").IsRequired();
        builder.Property(e => e.Format).HasColumnName("format").HasMaxLength(20);

        // Spectral representations
        builder.Property(e => e.Spectrogram).HasColumnName("spectrogram").HasColumnType("GEOMETRY");
        builder.Property(e => e.MelSpectrogram).HasColumnName("mel_spectrogram").HasColumnType("GEOMETRY");

        // Waveforms
        builder.Property(e => e.WaveformLeft).HasColumnName("waveform_left").HasColumnType("GEOMETRY");
        builder.Property(e => e.WaveformRight).HasColumnName("waveform_right").HasColumnType("GEOMETRY");

        // Vector representations
        builder.Property(e => e.GlobalEmbedding).HasColumnName("global_embedding").HasColumnType("VECTOR(768)");
        builder.Property(e => e.GlobalEmbeddingDim).HasColumnName("global_embedding_dim");

        // Metadata
        builder.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("JSON");

        builder.Property(e => e.IngestionDate).HasColumnName("ingestion_date").HasDefaultValueSql("SYSUTCDATETIME()");

        // Indexes
        builder.HasIndex(e => e.DurationMs).HasDatabaseName("idx_duration");
        builder.HasIndex(e => e.IngestionDate).HasDatabaseName("idx_ingestion").IsDescending();

        // Relationships
        builder.HasMany(e => e.Frames)
            .WithOne(f => f.Audio)
            .HasForeignKey(f => f.AudioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

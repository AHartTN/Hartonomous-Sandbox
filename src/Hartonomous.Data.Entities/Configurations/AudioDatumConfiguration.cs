using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AudioDatumConfiguration : IEntityTypeConfiguration<AudioDatum>
{
    public void Configure(EntityTypeBuilder<AudioDatum> builder)
    {
        builder.ToTable("AudioData", "dbo");
        builder.HasKey(e => new { e.AudioId });

        builder.Property(e => e.AudioId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.DurationMs)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.Format)
            .HasColumnType("nvarchar(20)")
            .HasMaxLength(20)
            ;

        builder.Property(e => e.GlobalEmbedding)
            .HasColumnType("vector(1998)")
            .HasMaxLength(1998)
            ;

        builder.Property(e => e.IngestionDate)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.MelSpectrogram)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.Metadata)
            .HasColumnType("json")
            ;

        builder.Property(e => e.NumChannels)
            .HasColumnType("tinyint")
            ;

        builder.Property(e => e.SampleRate)
            .HasColumnType("int")
            ;

        builder.Property(e => e.SourcePath)
            .HasColumnType("nvarchar(500)")
            .HasMaxLength(500)
            ;

        builder.Property(e => e.Spectrogram)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.WaveformLeft)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.WaveformRight)
            .HasColumnType("geometry")
            ;

        builder.HasIndex(e => new { e.DurationMs })
            .HasDatabaseName("IX_AudioData_DurationMs")
            ;

        builder.HasIndex(e => new { e.IngestionDate })
            .HasDatabaseName("IX_AudioData_IngestionDate")
            ;

        builder.HasIndex(e => new { e.Spectrogram })
            .HasDatabaseName("IX_AudioData_Spectrogram")
            ;
    }
}

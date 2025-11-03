using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public class GenerationStreamConfiguration : IEntityTypeConfiguration<GenerationStream>
{
    public void Configure(EntityTypeBuilder<GenerationStream> builder)
    {
        builder.ToTable("GenerationStreams", "provenance");

        builder.HasKey(x => x.StreamId).HasName("PK_GenerationStreams");

        builder.Property(x => x.StreamId)
            .ValueGeneratedNever();

        builder.Property(x => x.Scope)
            .HasMaxLength(128)
            .IsUnicode(true);

        builder.Property(x => x.Model)
            .HasMaxLength(128)
            .IsUnicode(true);

        builder.Property(x => x.CreatedUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.Stream)
            .HasColumnType("provenance.AtomicStream")
            .IsRequired();

        builder.Property(x => x.PayloadSizeBytes)
            .HasColumnType("bigint")
            .ValueGeneratedOnAddOrUpdate()
            .HasComputedColumnSql("CONVERT(BIGINT, DATALENGTH([Stream]))", stored: true);

        builder.HasIndex(x => x.Scope)
            .HasDatabaseName("IX_GenerationStreams_Scope");

        builder.HasIndex(x => x.Model)
            .HasDatabaseName("IX_GenerationStreams_Model");
    }
}

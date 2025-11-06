using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public class GenerationStreamConfiguration : IEntityTypeConfiguration<GenerationStream>
{
    public void Configure(EntityTypeBuilder<GenerationStream> builder)
    {
        builder.ToTable("GenerationStreams", "provenance");

        // Primary key: GUID-based for distributed scenarios
        builder.HasKey(x => x.StreamId)
            .HasName("PK_GenerationStreams");

        builder.Property(x => x.StreamId)
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever(); // Client generates GUIDs

        // Auto-incrementing ID for easier SQL queries/joins
        builder.Property(x => x.GenerationStreamId)
            .HasColumnType("bigint")
            .UseIdentityColumn();

        // Model relationship
        builder.Property(x => x.ModelId)
            .HasColumnType("int");

        builder.Property(x => x.Scope)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128);

        builder.Property(x => x.Model)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128);

        builder.Property(x => x.GeneratedAtomIds)
            .HasColumnType("nvarchar(max)");

        // ProvenanceStream: Store AtomicStream UDT as binary
        builder.Property(x => x.ProvenanceStream)
            .HasColumnType("varbinary(max)");

        builder.Property(x => x.ContextMetadata)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.TenantId)
            .HasColumnType("int")
            .HasDefaultValue(0);

        builder.Property(x => x.CreatedUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // Indexes
        builder.HasIndex(x => x.GenerationStreamId)
            .HasDatabaseName("IX_GenerationStreams_GenerationStreamId");

        builder.HasIndex(x => x.Scope)
            .HasDatabaseName("IX_GenerationStreams_Scope");

        builder.HasIndex(x => x.Model)
            .HasDatabaseName("IX_GenerationStreams_Model");

        builder.HasIndex(x => x.ModelId)
            .HasDatabaseName("IX_GenerationStreams_ModelId");

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("IX_GenerationStreams_TenantId");

        builder.HasIndex(x => x.CreatedUtc)
            .HasDatabaseName("IX_GenerationStreams_CreatedUtc");

        // Foreign key to Models table
        builder.HasOne(x => x.ModelNavigation)
            .WithMany()
            .HasForeignKey(x => x.ModelId)
            .HasConstraintName("FK_GenerationStreams_Models")
            .OnDelete(DeleteBehavior.NoAction); // No cascade delete
    }
}

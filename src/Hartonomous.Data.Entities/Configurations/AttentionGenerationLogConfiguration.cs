using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AttentionGenerationLogConfiguration : IEntityTypeConfiguration<AttentionGenerationLog>
{
    public void Configure(EntityTypeBuilder<AttentionGenerationLog> builder)
    {
        builder.ToTable("AttentionGenerationLog", "dbo");
        builder.HasKey(e => new { e.Id });

        builder.Property(e => e.Id)
            .HasColumnType("int")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.AttentionHeads)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ContextJson)
            .HasColumnType("json")
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.DurationMs)
            .HasColumnType("int")
            ;

        builder.Property(e => e.GeneratedAtomIds)
            .HasColumnType("json")
            ;

        builder.Property(e => e.GenerationStreamId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.InputAtomIds)
            .HasColumnType("json")
            .IsRequired()
            ;

        builder.Property(e => e.MaxTokens)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ModelId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.Temperature)
            .HasColumnType("float")
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.TopK)
            .HasColumnType("int")
            ;

        builder.Property(e => e.TopP)
            .HasColumnType("float")
            ;

        builder.HasOne(d => d.Model)
            .WithMany(p => p.AttentionGenerationLogs)
            .HasForeignKey(d => new { d.ModelId })
            ;

        builder.HasIndex(e => new { e.CreatedAt })
            .HasDatabaseName("IX_AttentionGenerationLog_CreatedAt")
            ;

        builder.HasIndex(e => new { e.GenerationStreamId })
            .HasDatabaseName("IX_AttentionGenerationLog_GenerationStreamId")
            ;

        builder.HasIndex(e => new { e.ModelId })
            .HasDatabaseName("IX_AttentionGenerationLog_ModelId")
            ;
    }
}

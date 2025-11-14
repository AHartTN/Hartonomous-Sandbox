using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class InferenceCacheInMemoryConfiguration : IEntityTypeConfiguration<InferenceCacheInMemory>
{
    public void Configure(EntityTypeBuilder<InferenceCacheInMemory> builder)
    {
        builder.ToTable("InferenceCache_InMemory", "dbo");
        builder.HasKey(e => new { e.CacheId });

        builder.Property(e => e.CacheId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.AccessCount)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.CacheKey)
            .HasColumnType("nvarchar(64)")
            .HasMaxLength(64)
            .IsRequired()
            ;

        builder.Property(e => e.ComputeTimeMs)
            .HasColumnType("float")
            ;

        builder.Property(e => e.CreatedUtc)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.InferenceType)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired()
            ;

        builder.Property(e => e.InputHash)
            .HasColumnType("binary(32)")
            .HasMaxLength(32)
            .IsRequired()
            ;

        builder.Property(e => e.IntermediateStates)
            .HasColumnType("varbinary(max)")
            ;

        builder.Property(e => e.LastAccessedUtc)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.ModelId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.OutputData)
            .HasColumnType("varbinary(max)")
            .IsRequired()
            ;

        builder.Property(e => e.SizeBytes)
            .HasColumnType("bigint")
            ;

        builder.HasIndex(e => new { e.CacheKey })
            .HasDatabaseName("IX_CacheKey_Hash")
            ;

        builder.HasIndex(e => new { e.LastAccessedUtc })
            .HasDatabaseName("IX_LastAccessed_Range")
            ;

        builder.HasIndex(e => new { e.ModelId, e.InputHash })
            .HasDatabaseName("IX_ModelInput_Hash")
            ;
    }
}

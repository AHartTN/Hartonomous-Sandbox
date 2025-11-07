using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public class InferenceCacheConfiguration : IEntityTypeConfiguration<InferenceCache>
{
    public void Configure(EntityTypeBuilder<InferenceCache> builder)
    {
        builder.ToTable("InferenceCache");

        builder.HasKey(e => e.CacheId);

        builder.Property(e => e.CacheId)
            .UseIdentityColumn();

        builder.Property(e => e.CreatedUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(e => e.AccessCount)
            .HasDefaultValue(0L);

        // Indexes
        builder.HasIndex(e => e.CacheKey)
            .HasDatabaseName("IX_InferenceCache_CacheKey");

        builder.HasIndex(e => new { e.ModelId, e.InferenceType })
            .HasDatabaseName("IX_InferenceCache_ModelId_InferenceType");

        builder.HasIndex(e => e.LastAccessedUtc)
            .HasDatabaseName("IX_InferenceCache_LastAccessedUtc")
            .IsDescending();

        // Foreign key
        builder.HasOne(e => e.Model)
            .WithMany()
            .HasForeignKey(e => e.ModelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
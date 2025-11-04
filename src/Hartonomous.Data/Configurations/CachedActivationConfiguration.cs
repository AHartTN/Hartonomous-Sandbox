using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

public class CachedActivationConfiguration : IEntityTypeConfiguration<CachedActivation>
{
    public void Configure(EntityTypeBuilder<CachedActivation> builder)
    {
        builder.ToTable("CachedActivations");

        builder.HasKey(ca => ca.CacheId);

        builder.Property(ca => ca.InputHash)
            .IsRequired()
            .HasMaxLength(32) // SHA256 = 32 bytes
            .HasColumnType("binary(32)");

        // Activation output as VECTOR (layer outputs are float arrays)
        // For large activations (> 1998 dims), chunk into multiple rows
        builder.Property(ca => ca.ActivationOutput)
            .HasColumnType("VECTOR(1998)");  // Max dimension for float32

        builder.Property(ca => ca.OutputShape)
            .HasMaxLength(100);

        builder.Property(ca => ca.HitCount)
            .HasDefaultValue(0);

        builder.Property(ca => ca.CreatedDate)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(ca => ca.LastAccessed)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(ca => ca.ComputeTimeSavedMs)
            .HasDefaultValue(0);

        // Unique index for cache lookup
        builder.HasIndex(ca => new { ca.ModelId, ca.LayerId, ca.InputHash })
            .IsUnique()
            .HasDatabaseName("IX_CachedActivations_Model_Layer_InputHash");

        // Index for cache eviction (LRU)
        builder.HasIndex(ca => new { ca.LastAccessed, ca.HitCount })
            .HasDatabaseName("IX_CachedActivations_LastAccessed_HitCount")
            .IsDescending(true, true);

        // Relationships - NoAction on Model to avoid cascade path conflicts
        // CachedActivation -> Layer -> Model creates cascade path
        builder.HasOne(ca => ca.Model)
            .WithMany()
            .HasForeignKey(ca => ca.ModelId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

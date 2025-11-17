using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Entities.Configurations;

public class InferenceCacheConfiguration : IEntityTypeConfiguration<InferenceCache>
{
    public void Configure(EntityTypeBuilder<InferenceCache> builder)
    {
        builder.ToTable("InferenceCache", "dbo");
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
            .HasColumnType("varbinary(max)")
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

        builder.HasOne(d => d.Model)
            .WithMany(p => p.InferenceCaches)
            .HasForeignKey(d => new { d.ModelId })
            ;
    }
}

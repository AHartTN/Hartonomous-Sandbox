using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class CachedActivationsInMemoryConfiguration : IEntityTypeConfiguration<CachedActivationsInMemory>
{
    public void Configure(EntityTypeBuilder<CachedActivationsInMemory> builder)
    {
        builder.ToTable("CachedActivations_InMemory", "dbo");
        builder.HasKey(e => new { e.CacheId });

        builder.Property(e => e.CacheId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.ActivationOutput)
            .HasColumnType("varbinary(max)")
            ;

        builder.Property(e => e.ComputeTimeSavedMs)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.CreatedDate)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.HitCount)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.InputHash)
            .HasColumnType("binary(32)")
            .HasMaxLength(32)
            .IsRequired()
            ;

        builder.Property(e => e.LastAccessed)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.LayerId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.ModelId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.OutputShape)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            ;

        builder.HasIndex(e => new { e.LastAccessed })
            .HasDatabaseName("IX_LastAccessed_Range")
            ;

        builder.HasIndex(e => new { e.LayerId, e.InputHash })
            .HasDatabaseName("IX_LayerInput_Hash")
            ;

        builder.HasIndex(e => new { e.ModelId })
            .HasDatabaseName("IX_ModelId_Hash")
            ;
    }
}

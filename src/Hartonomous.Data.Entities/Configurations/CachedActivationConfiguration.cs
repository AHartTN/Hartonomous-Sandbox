using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class CachedActivationConfiguration : IEntityTypeConfiguration<CachedActivation>
{
    public void Configure(EntityTypeBuilder<CachedActivation> builder)
    {
        builder.ToTable("CachedActivations", "dbo");
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

        builder.HasOne(d => d.Layer)
            .WithMany(p => p.CachedActivations)
            .HasForeignKey(d => new { d.LayerId })
            ;

        builder.HasOne(d => d.Model)
            .WithMany(p => p.CachedActivations)
            .HasForeignKey(d => new { d.ModelId })
            ;

        builder.HasIndex(e => new { e.LastAccessed, e.HitCount })
            .HasDatabaseName("IX_CachedActivations_LastAccessed_HitCount")
            ;

        builder.HasIndex(e => new { e.LayerId })
            .HasDatabaseName("IX_CachedActivations_LayerId")
            ;

        builder.HasIndex(e => new { e.ModelId, e.LayerId, e.InputHash })
            .HasDatabaseName("IX_CachedActivations_Model_Layer_InputHash")
            .IsUnique()
            ;
    }
}

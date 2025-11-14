using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class DeduplicationPolicyConfiguration : IEntityTypeConfiguration<DeduplicationPolicy>
{
    public void Configure(EntityTypeBuilder<DeduplicationPolicy> builder)
    {
        builder.ToTable("DeduplicationPolicies", "dbo");
        builder.HasKey(e => new { e.DeduplicationPolicyId });

        builder.Property(e => e.DeduplicationPolicyId)
            .HasColumnType("int")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.IsActive)
            .HasColumnType("bit")
            ;

        builder.Property(e => e.Metadata)
            .HasColumnType("json")
            ;

        builder.Property(e => e.PolicyName)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            .IsRequired()
            ;

        builder.Property(e => e.SemanticThreshold)
            .HasColumnType("float")
            ;

        builder.Property(e => e.SpatialThreshold)
            .HasColumnType("float")
            ;

        builder.HasIndex(e => new { e.PolicyName })
            .HasDatabaseName("UX_DeduplicationPolicies_PolicyName")
            .IsUnique()
            ;
    }
}

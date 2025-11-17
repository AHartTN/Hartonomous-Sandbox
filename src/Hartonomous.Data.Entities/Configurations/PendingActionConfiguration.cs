using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Entities.Configurations;

public class PendingActionConfiguration : IEntityTypeConfiguration<PendingAction>
{
    public void Configure(EntityTypeBuilder<PendingAction> builder)
    {
        builder.ToTable("PendingActions", "dbo");
        builder.HasKey(e => new { e.ActionId });

        builder.Property(e => e.ActionId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.ActionType)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired()
            ;

        builder.Property(e => e.ApprovedBy)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            ;

        builder.Property(e => e.ApprovedUtc)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.CreatedUtc)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Description)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.ErrorMessage)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.EstimatedImpact)
            .HasColumnType("nvarchar(20)")
            .HasMaxLength(20)
            ;

        builder.Property(e => e.ExecutedUtc)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Parameters)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.Priority)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ResultJson)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.RiskLevel)
            .HasColumnType("nvarchar(20)")
            .HasMaxLength(20)
            .IsRequired()
            ;

        builder.Property(e => e.SqlStatement)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.Status)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.HasIndex(e => new { e.CreatedUtc })
            .HasDatabaseName("IX_PendingActions_Created")
            ;

        builder.HasIndex(e => new { e.Priority, e.CreatedUtc })
            .HasDatabaseName("IX_PendingActions_Priority")
            ;

        builder.HasIndex(e => new { e.Status })
            .HasDatabaseName("IX_PendingActions_Status")
            ;
    }
}

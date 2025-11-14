using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class StatusConfiguration : IEntityTypeConfiguration<Status>
{
    public void Configure(EntityTypeBuilder<Status> builder)
    {
        builder.ToTable("Status", "ref");
        builder.HasKey(e => new { e.StatusId });

        builder.Property(e => e.StatusId)
            .HasColumnType("int")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.Code)
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Description)
            .HasColumnType("nvarchar(500)")
            .HasMaxLength(500)
            ;

        builder.Property(e => e.IsActive)
            .HasColumnType("bit")
            ;

        builder.Property(e => e.Name)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired()
            ;

        builder.Property(e => e.SortOrder)
            .HasColumnType("int")
            ;

        builder.Property(e => e.UpdatedAt)
            .HasColumnType("datetime2")
            ;

        builder.HasIndex(e => new { e.IsActive, e.Code })
            .HasDatabaseName("IX_Status_Active_Code")
            ;

        builder.HasIndex(e => new { e.Code })
            .HasDatabaseName("UQ_Status_Code")
            .IsUnique()
            ;

        builder.HasIndex(e => new { e.Name })
            .HasDatabaseName("UQ_Status_Name")
            .IsUnique()
            ;
    }
}

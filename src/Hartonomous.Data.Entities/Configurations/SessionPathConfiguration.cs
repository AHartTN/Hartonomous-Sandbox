using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class SessionPathConfiguration : IEntityTypeConfiguration<SessionPath>
{
    public void Configure(EntityTypeBuilder<SessionPath> builder)
    {
        builder.ToTable("SessionPaths", "dbo");
        builder.HasKey(e => new { e.SessionPathId });

        builder.Property(e => e.SessionPathId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.EndTime)
            .HasColumnType("float")
            ;

        builder.Property(e => e.Path)
            .HasColumnType("geometry")
            .IsRequired()
            ;

        builder.Property(e => e.PathLength)
            .HasColumnType("float")
            ;

        builder.Property(e => e.SessionId)
            .HasColumnType("uniqueidentifier")
            ;

        builder.Property(e => e.StartTime)
            .HasColumnType("float")
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            ;

        builder.HasIndex(e => new { e.SessionId })
            .HasDatabaseName("IX_SessionPaths_SessionId")
            .IsUnique()
            ;
    }
}

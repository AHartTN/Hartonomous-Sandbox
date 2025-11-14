using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class SessionPathsInMemoryConfiguration : IEntityTypeConfiguration<SessionPathsInMemory>
{
    public void Configure(EntityTypeBuilder<SessionPathsInMemory> builder)
    {
        builder.ToTable("SessionPaths_InMemory", "dbo");
        builder.HasKey(e => new { e.SessionPathId });

        builder.Property(e => e.SessionPathId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.CreatedUtc)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.HypothesisId)
            .HasColumnType("uniqueidentifier")
            ;

        builder.Property(e => e.IsSelected)
            .HasColumnType("bit")
            ;

        builder.Property(e => e.PathNumber)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ResponseText)
            .HasColumnType("nvarchar(max)")
            ;

        builder.Property(e => e.ResponseVector)
            .HasColumnType("varbinary(max)")
            ;

        builder.Property(e => e.Score)
            .HasColumnType("float")
            ;

        builder.Property(e => e.SessionId)
            .HasColumnType("uniqueidentifier")
            ;

        builder.HasIndex(e => new { e.SessionId })
            .HasDatabaseName("IX_SessionId_Hash")
            ;

        builder.HasIndex(e => new { e.SessionId, e.PathNumber })
            .HasDatabaseName("IX_SessionPath_Hash")
            ;
    }
}

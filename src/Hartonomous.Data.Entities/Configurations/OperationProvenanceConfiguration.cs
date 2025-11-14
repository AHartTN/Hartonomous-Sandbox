using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class OperationProvenanceConfiguration : IEntityTypeConfiguration<OperationProvenance>
{
    public void Configure(EntityTypeBuilder<OperationProvenance> builder)
    {
        builder.ToTable("OperationProvenance", "dbo");
        builder.HasKey(e => new { e.Id });

        builder.Property(e => e.Id)
            .HasColumnType("int")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.OperationId)
            .HasColumnType("uniqueidentifier")
            ;

        builder.HasIndex(e => new { e.CreatedAt })
            .HasDatabaseName("IX_OperationProvenance_CreatedAt")
            ;

        builder.HasIndex(e => new { e.OperationId })
            .HasDatabaseName("IX_OperationProvenance_OperationId")
            ;

        builder.HasIndex(e => new { e.OperationId })
            .HasDatabaseName("UQ__tmp_ms_x__A4F5FC45624FE83C")
            .IsUnique()
            ;
    }
}

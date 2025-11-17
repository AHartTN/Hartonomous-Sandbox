using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Entities.Configurations;

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
            .HasDatabaseName("UQ__Operatio__A4F5FC4568BFD32C")
            .IsUnique()
            ;
    }
}

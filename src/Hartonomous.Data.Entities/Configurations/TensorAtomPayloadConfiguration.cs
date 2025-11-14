using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class TensorAtomPayloadConfiguration : IEntityTypeConfiguration<TensorAtomPayload>
{
    public void Configure(EntityTypeBuilder<TensorAtomPayload> builder)
    {
        builder.ToTable("TensorAtomPayloads", "dbo");
        builder.HasKey(e => new { e.PayloadId });

        builder.Property(e => e.PayloadId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Metadata)
            .HasColumnType("json")
            ;

        builder.Property(e => e.Payload)
            .HasColumnType("varbinary(max)")
            ;

        builder.Property(e => e.RowGuid)
            .HasColumnType("uniqueidentifier")
            ;

        builder.Property(e => e.TensorAtomId)
            .HasColumnType("bigint")
            ;

        builder.HasOne(d => d.TensorAtom)
            .WithOne(p => p.TensorAtomPayload)
            .HasForeignKey<TensorAtomPayload>(e => new { e.TensorAtomId })
            ;

        builder.HasIndex(e => new { e.TensorAtomId })
            .HasDatabaseName("IX_TensorAtomPayloads_TensorAtomId")
            .IsUnique()
            ;

        builder.HasIndex(e => new { e.RowGuid })
            .HasDatabaseName("UQ_TensorAtomPayloads_RowGuid")
            .IsUnique()
            ;
    }
}

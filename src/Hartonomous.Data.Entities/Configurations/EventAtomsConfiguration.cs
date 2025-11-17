using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class EventAtomsConfiguration : IEntityTypeConfiguration<EventAtoms>
{
    public void Configure(EntityTypeBuilder<EventAtoms> builder)
    {
        builder.ToTable("EventAtoms", "dbo");
        builder.HasKey(e => new { e.Id });

        builder.Property(e => e.Id)
            .HasColumnType("int")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.AverageWeight)
            .HasColumnType("float")
            ;

        builder.Property(e => e.CentroidAtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.ClusterId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.ClusterSize)
            .HasColumnType("int")
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.EventType)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired()
            ;

        builder.Property(e => e.StreamId)
            .HasColumnType("int")
            ;

        builder.HasOne(d => d.CentroidAtom)
            .WithMany(p => p.EventAtoms)
            .HasForeignKey(d => new { d.CentroidAtomId })
            ;

        builder.HasOne(d => d.Stream)
            .WithMany(p => p.EventAtoms)
            .HasForeignKey(d => new { d.StreamId })
            ;

        builder.HasIndex(e => new { e.ClusterId })
            .HasDatabaseName("IX_EventAtoms_ClusterId")
            ;

        builder.HasIndex(e => new { e.CreatedAt })
            .HasDatabaseName("IX_EventAtoms_CreatedAt")
            ;

        builder.HasIndex(e => new { e.EventType })
            .HasDatabaseName("IX_EventAtoms_EventType")
            ;

        builder.HasIndex(e => new { e.StreamId })
            .HasDatabaseName("IX_EventAtoms_StreamId")
            ;
    }
}

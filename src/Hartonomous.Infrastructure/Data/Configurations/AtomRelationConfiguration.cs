using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Infrastructure.Data.Configurations;

public class AtomRelationConfiguration : IEntityTypeConfiguration<AtomRelation>
{
    public void Configure(EntityTypeBuilder<AtomRelation> builder)
    {
        builder.ToTable("AtomRelations");

        builder.HasKey(r => r.AtomRelationId);

        builder.Property(r => r.RelationType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(r => r.Weight)
            .HasColumnType("real");

        builder.Property(r => r.SpatialExpression)
            .HasColumnType("geometry");

        builder.Property(r => r.Metadata)
            .HasColumnType("JSON");

        builder.Property(r => r.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(r => new { r.SourceAtomId, r.TargetAtomId, r.RelationType })
            .HasDatabaseName("IX_AtomRelations_Source_Target_Type");

        builder.HasOne(r => r.SourceAtom)
            .WithMany(a => a.SourceRelations)
            .HasForeignKey(r => r.SourceAtomId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.TargetAtom)
            .WithMany(a => a.TargetRelations)
            .HasForeignKey(r => r.TargetAtomId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

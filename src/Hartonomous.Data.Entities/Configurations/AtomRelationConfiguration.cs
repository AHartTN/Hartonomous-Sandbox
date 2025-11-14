using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AtomRelationConfiguration : IEntityTypeConfiguration<AtomRelation>
{
    public void Configure(EntityTypeBuilder<AtomRelation> builder)
    {
        builder.ToTable("AtomRelations", "dbo");
        builder.HasKey(e => new { e.AtomRelationId });

        builder.Property(e => e.AtomRelationId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.Confidence)
            .HasColumnType("real")
            ;

        builder.Property(e => e.CoordT)
            .HasColumnType("float")
            ;

        builder.Property(e => e.CoordW)
            .HasColumnType("float")
            ;

        builder.Property(e => e.CoordX)
            .HasColumnType("float")
            ;

        builder.Property(e => e.CoordY)
            .HasColumnType("float")
            ;

        builder.Property(e => e.CoordZ)
            .HasColumnType("float")
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Importance)
            .HasColumnType("real")
            ;

        builder.Property(e => e.Metadata)
            .HasColumnType("json")
            ;

        builder.Property(e => e.RelationType)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            .IsRequired()
            ;

        builder.Property(e => e.SequenceIndex)
            .HasColumnType("int")
            ;

        builder.Property(e => e.SourceAtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.SpatialBucket)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.SpatialBucketX)
            .HasColumnType("int")
            ;

        builder.Property(e => e.SpatialBucketY)
            .HasColumnType("int")
            ;

        builder.Property(e => e.SpatialBucketZ)
            .HasColumnType("int")
            ;

        builder.Property(e => e.SpatialExpression)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.TargetAtomId)
            .HasColumnType("bigint")
            ;

        builder.Property(e => e.TenantId)
            .HasColumnType("int")
            ;

        builder.Property(e => e.Weight)
            .HasColumnType("real")
            ;

        builder.HasOne(d => d.SourceAtom)
            .WithMany(p => p.AtomRelationSourceAtoms)
            .HasForeignKey(d => new { d.SourceAtomId })
            ;

        builder.HasOne(d => d.TargetAtom)
            .WithMany(p => p.AtomRelationTargetAtoms)
            .HasForeignKey(d => new { d.TargetAtomId })
            ;

        builder.HasIndex(e => new { e.RelationType })
            .HasDatabaseName("IX_AtomRelations_RelationType")
            ;

        builder.HasIndex(e => new { e.SourceAtomId, e.SequenceIndex })
            .HasDatabaseName("IX_AtomRelations_SequenceIndex")
            ;

        builder.HasIndex(e => new { e.SourceAtomId, e.TargetAtomId })
            .HasDatabaseName("IX_AtomRelations_SourceTarget")
            ;

        builder.HasIndex(e => new { e.SourceAtomId, e.TargetAtomId, e.RelationType })
            .HasDatabaseName("IX_AtomRelations_Source_Target_Type")
            ;

        builder.HasIndex(e => new { e.SpatialBucket })
            .HasDatabaseName("IX_AtomRelations_SpatialBucket")
            ;

        builder.HasIndex(e => new { e.TargetAtomId })
            .HasDatabaseName("IX_AtomRelations_TargetAtomId")
            ;

        builder.HasIndex(e => new { e.TargetAtomId, e.SourceAtomId })
            .HasDatabaseName("IX_AtomRelations_TargetSource")
            ;

        builder.HasIndex(e => new { e.TenantId, e.RelationType })
            .HasDatabaseName("IX_AtomRelations_Tenant")
            ;
    }
}

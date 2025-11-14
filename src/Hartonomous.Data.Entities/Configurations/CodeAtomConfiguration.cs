using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class CodeAtomConfiguration : IEntityTypeConfiguration<CodeAtom>
{
    public void Configure(EntityTypeBuilder<CodeAtom> builder)
    {
        builder.ToTable("CodeAtoms", "dbo");
        builder.HasKey(e => new { e.CodeAtomId });

        builder.Property(e => e.CodeAtomId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.Code)
            .HasColumnType("text")
            .IsRequired()
            ;

        builder.Property(e => e.CodeHash)
            .HasColumnType("varbinary(32)")
            .HasMaxLength(32)
            ;

        builder.Property(e => e.CodeType)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.CreatedBy)
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            ;

        builder.Property(e => e.Description)
            .HasColumnType("nvarchar(2000)")
            .HasMaxLength(2000)
            ;

        builder.Property(e => e.Embedding)
            .HasColumnType("geometry")
            ;

        builder.Property(e => e.EmbeddingDimension)
            .HasColumnType("int")
            ;

        builder.Property(e => e.Framework)
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            ;

        builder.Property(e => e.Language)
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired()
            ;

        builder.Property(e => e.QualityScore)
            .HasColumnType("real")
            ;

        builder.Property(e => e.SourceUri)
            .HasColumnType("nvarchar(2048)")
            .HasMaxLength(2048)
            ;

        builder.Property(e => e.Tags)
            .HasColumnType("json")
            ;

        builder.Property(e => e.TestResults)
            .HasColumnType("json")
            ;

        builder.Property(e => e.UpdatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.UsageCount)
            .HasColumnType("int")
            ;

        builder.HasIndex(e => new { e.CodeHash })
            .HasDatabaseName("IX_CodeAtoms_CodeHash")
            .IsUnique()
            ;

        builder.HasIndex(e => new { e.CodeType })
            .HasDatabaseName("IX_CodeAtoms_CodeType")
            ;

        builder.HasIndex(e => new { e.CreatedAt })
            .HasDatabaseName("IX_CodeAtoms_CreatedAt")
            ;

        builder.HasIndex(e => new { e.Embedding })
            .HasDatabaseName("IX_CodeAtoms_Embedding")
            ;

        builder.HasIndex(e => new { e.Language })
            .HasDatabaseName("IX_CodeAtoms_Language")
            ;

        builder.HasIndex(e => new { e.QualityScore })
            .HasDatabaseName("IX_CodeAtoms_QualityScore")
            ;
    }
}

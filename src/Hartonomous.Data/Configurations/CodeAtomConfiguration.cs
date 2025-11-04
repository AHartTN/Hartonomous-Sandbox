using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Configurations;

/// <summary>
/// EF Core configuration for CodeAtom entity.
/// Maps to SQL Server 2025 with VECTOR(1998) embeddings stored as GEOMETRY.
/// </summary>
public class CodeAtomConfiguration : IEntityTypeConfiguration<CodeAtom>
{
    public void Configure(EntityTypeBuilder<CodeAtom> builder)
    {
        builder.ToTable("CodeAtoms", schema: "dbo");

        builder.HasKey(c => c.CodeAtomId);

        builder.Property(c => c.CodeAtomId)
            .HasColumnName("CodeAtomId")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.Language)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("Language");

        builder.Property(c => c.Code)
            .IsRequired()
            .HasColumnType("TEXT") // Large T-SQL procedures can be huge
            .HasColumnName("Code");

        builder.Property(c => c.Framework)
            .HasMaxLength(200)
            .HasColumnName("Framework");

        builder.Property(c => c.Description)
            .HasMaxLength(2000)
            .HasColumnName("Description");

        builder.Property(c => c.CodeType)
            .HasMaxLength(100)
            .HasColumnName("CodeType");

        // Embedding stored as GEOMETRY LINESTRING ZM (compatible with VECTOR(1998))
        builder.Property(c => c.Embedding)
            .HasColumnName("Embedding")
            .HasColumnType("geometry");

        builder.Property(c => c.EmbeddingDimension)
            .HasColumnName("EmbeddingDimension");

        // JSON column for test results (SQL Server 2025 JSON type)
        builder.Property(c => c.TestResults)
            .HasColumnName("TestResults")
            .HasColumnType("JSON");

        builder.Property(c => c.QualityScore)
            .HasColumnName("QualityScore")
            .HasPrecision(5, 4); // 0.0000 to 1.0000

        builder.Property(c => c.UsageCount)
            .HasColumnName("UsageCount")
            .HasDefaultValue(0);

        builder.Property(c => c.CodeHash)
            .HasMaxLength(32) // SHA256 = 32 bytes
            .HasColumnName("CodeHash");

        builder.Property(c => c.SourceUri)
            .HasMaxLength(2048)
            .HasColumnName("SourceUri");

        // Tags as JSON array
        builder.Property(c => c.Tags)
            .HasColumnName("Tags")
            .HasColumnType("JSON");

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasColumnName("CreatedAt")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("UpdatedAt");

        builder.Property(c => c.CreatedBy)
            .HasMaxLength(200)
            .HasColumnName("CreatedBy");

        // Indexes for performance
        builder.HasIndex(c => c.Language)
            .HasDatabaseName("IX_CodeAtoms_Language");

        builder.HasIndex(c => c.CodeType)
            .HasDatabaseName("IX_CodeAtoms_CodeType");

        builder.HasIndex(c => c.CodeHash)
            .HasDatabaseName("IX_CodeAtoms_CodeHash")
            .IsUnique();

        builder.HasIndex(c => c.QualityScore)
            .HasDatabaseName("IX_CodeAtoms_QualityScore");

        builder.HasIndex(c => c.CreatedAt)
            .HasDatabaseName("IX_CodeAtoms_CreatedAt");

        // Spatial index for embeddings (created in SQL migration separately)
        builder.HasIndex(c => c.Embedding)
            .HasDatabaseName("IX_CodeAtoms_Embedding_Spatial");
    }
}

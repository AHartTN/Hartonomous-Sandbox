using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hartonomous.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Composite index for embeddings: frequently filtered by model and sorted by creation date
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Embeddings_ModelId_CreatedAt' AND object_id = OBJECT_ID('dbo.Embeddings_Production'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Embeddings_ModelId_CreatedAt
    ON dbo.Embeddings_Production(model_id, created_at DESC)
    INCLUDE (embedding_id, embedding_full);
END
");

            // Composite index for ingestion jobs: Status + Priority filtering
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IngestionJobs_Status_Priority' AND object_id = OBJECT_ID('dbo.IngestionJobs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_IngestionJobs_Status_Priority
    ON dbo.IngestionJobs(Status, CreatedAt DESC)
    WHERE Status IS NOT NULL;
END
");

            // Composite index for atom relations: AtomId + RelationType lookups
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AtomRelations_SourceAtom_Type' AND object_id = OBJECT_ID('dbo.AtomRelations'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AtomRelations_SourceAtom_Type
    ON dbo.AtomRelations(SourceAtomId, RelationType)
    INCLUDE (TargetAtomId, Weight, CreatedAt);
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AtomRelations_TargetAtom_Type' AND object_id = OBJECT_ID('dbo.AtomRelations'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AtomRelations_TargetAtom_Type
    ON dbo.AtomRelations(TargetAtomId, RelationType)
    INCLUDE (SourceAtomId, Weight, CreatedAt);
END
");

            // Composite index for atom embeddings: AtomId + EmbeddingType
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AtomEmbeddings_Atom_Type' AND object_id = OBJECT_ID('dbo.AtomEmbeddings'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AtomEmbeddings_Atom_Type
    ON dbo.AtomEmbeddings(AtomId, EmbeddingType)
    INCLUDE (AtomEmbeddingId, Dimension);
END
");

            // Composite index for tensor atoms: ModelId + LayerId + AtomType
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TensorAtoms_Model_Layer_Type' AND object_id = OBJECT_ID('dbo.TensorAtoms'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_TensorAtoms_Model_Layer_Type
    ON dbo.TensorAtoms(ModelId, LayerId, AtomType)
    INCLUDE (TensorAtomId, ImportanceScore, CreatedAt);
END
");

            // Index for atoms by content hash (duplicate detection)
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Atoms_ContentHash' AND object_id = OBJECT_ID('dbo.Atoms'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Atoms_ContentHash
    ON dbo.Atoms(ContentHash)
    INCLUDE (AtomId, AtomType, CreatedAt)
    WHERE ContentHash IS NOT NULL;
END
");

            // Index for deduplication policies: Active policies by name
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DeduplicationPolicies_Name_Active' AND object_id = OBJECT_ID('dbo.DeduplicationPolicies'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DeduplicationPolicies_Name_Active
    ON dbo.DeduplicationPolicies(PolicyName, IsActive)
    WHERE IsActive = 1;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Embeddings_ModelId_CreatedAt') DROP INDEX IX_Embeddings_ModelId_CreatedAt ON dbo.Embeddings_Production;");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IngestionJobs_Status_Priority') DROP INDEX IX_IngestionJobs_Status_Priority ON dbo.IngestionJobs;");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AtomRelations_SourceAtom_Type') DROP INDEX IX_AtomRelations_SourceAtom_Type ON dbo.AtomRelations;");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AtomRelations_TargetAtom_Type') DROP INDEX IX_AtomRelations_TargetAtom_Type ON dbo.AtomRelations;");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AtomEmbeddings_Atom_Type') DROP INDEX IX_AtomEmbeddings_Atom_Type ON dbo.AtomEmbeddings;");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TensorAtoms_Model_Layer_Type') DROP INDEX IX_TensorAtoms_Model_Layer_Type ON dbo.TensorAtoms;");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Atoms_ContentHash') DROP INDEX IX_Atoms_ContentHash ON dbo.Atoms;");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DeduplicationPolicies_Name_Active') DROP INDEX IX_DeduplicationPolicies_Name_Active ON dbo.DeduplicationPolicies;");
        }
    }
}

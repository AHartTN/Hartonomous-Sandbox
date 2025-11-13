-- =============================================
-- Full-Text Search Index on dbo.Atoms.Content (WITHOUT semantic search)
-- Semantic similarity now uses vector embeddings (AtomEmbeddings table)
-- =============================================

-- Create full-text catalog if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'HartonomousFullTextCatalog')
BEGIN
    CREATE FULLTEXT CATALOG HartonomousFullTextCatalog AS DEFAULT;
END
GO

-- Drop old semantic index if it exists
IF EXISTS (
    SELECT 1 
    FROM sys.fulltext_indexes fi
    INNER JOIN sys.fulltext_index_columns fic ON fi.object_id = fic.object_id
    WHERE fi.object_id = OBJECT_ID('dbo.Atoms')
    AND fic.statistical_semantics = 1
)
BEGIN
    DROP FULLTEXT INDEX ON dbo.Atoms;
END
GO

-- Create full-text index WITHOUT STATISTICAL_SEMANTICS
IF NOT EXISTS (
    SELECT 1 
    FROM sys.fulltext_indexes fi
    INNER JOIN sys.tables t ON fi.object_id = t.object_id
    WHERE t.name = 'Atoms' AND SCHEMA_NAME(t.schema_id) = 'dbo'
)
BEGIN
    CREATE FULLTEXT INDEX ON dbo.Atoms(Content LANGUAGE 1033)
    KEY INDEX PK_Atoms
    ON HartonomousFullTextCatalog
    WITH (CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM);
END
GO

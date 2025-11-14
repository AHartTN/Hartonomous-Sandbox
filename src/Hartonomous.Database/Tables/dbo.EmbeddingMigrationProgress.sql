CREATE TABLE [dbo].[EmbeddingMigrationProgress] (
    [AtomEmbeddingId] BIGINT        NOT NULL,
    [MigratedAt]      DATETIME2 (7) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [AtomCount]       INT           NOT NULL,
    [RelationCount]   INT           NOT NULL,
    CONSTRAINT [PK_EmbeddingMigrationProgress] PRIMARY KEY CLUSTERED ([AtomEmbeddingId] ASC)
);

CREATE TABLE [dbo].[AttentionGenerationLog]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ModelId] INT NOT NULL,
    [InputAtomIds] NVARCHAR(MAX) NOT NULL,
    [ContextJson] NVARCHAR(MAX) NULL,
    [MaxTokens] INT NOT NULL,
    [Temperature] FLOAT NOT NULL,
    [TopK] INT NOT NULL,
    [TopP] FLOAT NOT NULL,
    [AttentionHeads] INT NOT NULL,
    [GenerationStreamId] BIGINT NOT NULL,
    [DurationMs] INT NOT NULL,
    [TenantId] INT NOT NULL DEFAULT (0),
    [CreatedAt] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_AttentionGenerationLog] PRIMARY KEY CLUSTERED ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_AttentionGenerationLog_ModelId]
    ON [dbo].[AttentionGenerationLog]([ModelId]);
GO

CREATE NONCLUSTERED INDEX [IX_AttentionGenerationLog_GenerationStreamId]
    ON [dbo].[AttentionGenerationLog]([GenerationStreamId]);
GO

CREATE NONCLUSTERED INDEX [IX_AttentionGenerationLog_CreatedAt]
    ON [dbo].[AttentionGenerationLog]([CreatedAt] DESC);
GO
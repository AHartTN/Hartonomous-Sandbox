-- GenerationStreams table for storing inference provenance
-- Each row represents one generation operation with complete AtomicStream tracking

CREATE TABLE [provenance].[GenerationStreams]
(
    [GenerationStreamId] BIGINT IDENTITY(1,1) NOT NULL,
    [ModelId] INT NOT NULL,
    [GeneratedAtomIds] NVARCHAR(MAX) NOT NULL,
    [ProvenanceStream] [provenance].[AtomicStream] NOT NULL,
    [ContextMetadata] NVARCHAR(MAX) NULL,
    [TenantId] INT NOT NULL DEFAULT (0),
    [CreatedUtc] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_GenerationStreams] PRIMARY KEY CLUSTERED ([GenerationStreamId]),
    CONSTRAINT [FK_GenerationStreams_Models] FOREIGN KEY ([ModelId])
        REFERENCES [dbo].[Models]([ModelId]),
    CONSTRAINT [CK_GenerationStreams_ContextMetadata] 
        CHECK ([ContextMetadata] IS NULL OR ISJSON([ContextMetadata]) = 1)
);
GO

CREATE NONCLUSTERED INDEX [IX_GenerationStreams_ModelId]
    ON [provenance].[GenerationStreams]([ModelId]);
GO

CREATE NONCLUSTERED INDEX [IX_GenerationStreams_TenantId]
    ON [provenance].[GenerationStreams]([TenantId]);
GO

CREATE NONCLUSTERED INDEX [IX_GenerationStreams_CreatedUtc]
    ON [provenance].[GenerationStreams]([CreatedUtc]);
GO
GO
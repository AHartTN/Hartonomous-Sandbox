-- GenerationStreamSegments: Stores individual segments from generation streams
-- Used for attention mechanism embedding extraction
CREATE TABLE [dbo].[GenerationStreamSegments] (
    [SegmentId]          BIGINT          NOT NULL IDENTITY,
    [GenerationStreamId] BIGINT          NOT NULL,
    [SegmentOrdinal]     INT             NOT NULL,
    [SegmentKind]        NVARCHAR (50)   NOT NULL, -- 'Input', 'Output', 'Embedding', 'Control', etc.
    [ContentType]        NVARCHAR (128)  NULL,
    [Metadata]           NVARCHAR (MAX)  NULL,
    [PayloadData]        VARBINARY (MAX) NULL,
    [EmbeddingVector]    VARBINARY (MAX) NULL, -- Extracted embedding from segment
    [CreatedAt]          DATETIME2 (7)   NOT NULL DEFAULT (SYSUTCDATETIME()),
    [TenantId]           INT             NOT NULL DEFAULT (0),
    CONSTRAINT [PK_GenerationStreamSegments] PRIMARY KEY CLUSTERED ([SegmentId] ASC),
    CONSTRAINT [FK_GenerationStreamSegments_GenerationStreams] FOREIGN KEY ([GenerationStreamId]) 
        REFERENCES [provenance].[GenerationStreams] ([GenerationStreamId]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_GenerationStreamSegments_GenerationStreamId] 
    ON [dbo].[GenerationStreamSegments] ([GenerationStreamId], [SegmentOrdinal]);
GO

CREATE NONCLUSTERED INDEX [IX_GenerationStreamSegments_CreatedAt] 
    ON [dbo].[GenerationStreamSegments] ([CreatedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_GenerationStreamSegments_SegmentKind] 
    ON [dbo].[GenerationStreamSegments] ([SegmentKind]) 
    WHERE [EmbeddingVector] IS NOT NULL;
GO

CREATE TABLE [provenance].[GenerationStreams] (
    [StreamId]           UNIQUEIDENTIFIER NOT NULL,
    [GenerationStreamId] BIGINT           NOT NULL IDENTITY,
    [ModelId]            INT              NULL,
    [Scope]              NVARCHAR (128)   NULL,
    [Model]              NVARCHAR (128)   NULL,
    [GeneratedAtomIds]   NVARCHAR (MAX)   NULL,
    [ProvenanceStream]   VARBINARY (MAX)  NULL,
    [ContextMetadata]    NVARCHAR (MAX)   NULL,
    [TenantId]           INT              NOT NULL DEFAULT 0,
    [CreatedUtc]         DATETIME2 (3)    NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_GenerationStreams] PRIMARY KEY CLUSTERED ([StreamId] ASC),
    CONSTRAINT [FK_GenerationStreams_Models] FOREIGN KEY ([ModelId]) REFERENCES [dbo].[Models] ([ModelId])
);
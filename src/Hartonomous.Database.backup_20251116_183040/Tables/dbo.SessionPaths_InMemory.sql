CREATE TABLE [dbo].[SessionPaths_InMemory]
(
    [SessionPathId]  BIGINT           IDENTITY (1, 1) NOT NULL,
    [SessionId]      UNIQUEIDENTIFIER NOT NULL,
    [PathNumber]     INT              NOT NULL,
    [HypothesisId]   UNIQUEIDENTIFIER NULL,
    [ResponseText]   NVARCHAR (MAX)   COLLATE Latin1_General_100_BIN2 NULL,
    [ResponseVector] VARBINARY (MAX)  NULL,
    [Score]          FLOAT            NULL,
    [IsSelected]     BIT              NOT NULL DEFAULT (0),
    [CreatedUtc]     DATETIME2 (7)    NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_SessionPaths_InMemory] PRIMARY KEY NONCLUSTERED ([SessionPathId]),
    INDEX [IX_SessionId_Hash] HASH ([SessionId]) WITH (BUCKET_COUNT = 200000),
    INDEX [IX_SessionPath_Hash] HASH ([SessionId], [PathNumber]) WITH (BUCKET_COUNT = 200000)
)
WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA);

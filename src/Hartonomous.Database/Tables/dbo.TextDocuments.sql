CREATE TABLE [dbo].[TextDocuments] (
    [DocId]              BIGINT          NOT NULL IDENTITY,
    [SourcePath]         NVARCHAR (500)  NULL,
    [SourceUrl]          NVARCHAR (1000) NULL,
    [RawText]            NVARCHAR (MAX)  NOT NULL,
    [Language]           NVARCHAR (10)   NULL,
    [CharCount]          INT             NULL,
    [WordCount]          INT             NULL,
    [GlobalEmbedding]    VECTOR(1998)    NULL,
    [TopicVector]        VECTOR(1998)    NULL,
    [SentimentScore]     REAL            NULL,
    [Toxicity]           REAL            NULL,
    [Metadata]           JSON   NULL,
    [IngestionDate]      DATETIME2 (7)   NULL DEFAULT (SYSUTCDATETIME()),
    [LastAccessed]       DATETIME2 (7)   NULL,
    [AccessCount]        BIGINT          NOT NULL DEFAULT CAST(0 AS BIGINT),
    CONSTRAINT [PK_TextDocuments] PRIMARY KEY CLUSTERED ([DocId] ASC)
);

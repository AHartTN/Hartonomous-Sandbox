-- =============================================
-- Table: dbo.TextDocuments
-- =============================================
-- Represents a text document with embeddings and semantic features for NLP tasks.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.TextDocuments', 'U') IS NOT NULL
    DROP TABLE dbo.TextDocuments;
GO

CREATE TABLE dbo.TextDocuments
(
    DocId               BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    SourcePath          NVARCHAR(500)   NULL,
    SourceUrl           NVARCHAR(1000)  NULL,
    RawText             NVARCHAR(MAX)   NOT NULL,
    Language            NVARCHAR(10)    NULL,
    CharCount           INT             NULL,
    WordCount           INT             NULL,
    GlobalEmbedding     VECTOR(768)     NULL,
    GlobalEmbeddingDim  INT             NULL,
    TopicVector         VECTOR(100)     NULL,
    SentimentScore      REAL            NULL,
    Toxicity            REAL            NULL,
    Metadata            NVARCHAR(MAX)   NULL,
    IngestionDate       DATETIME2       NULL DEFAULT SYSUTCDATETIME(),
    LastAccessed        DATETIME2       NULL,
    AccessCount         BIGINT          NOT NULL DEFAULT 0,

    CONSTRAINT CK_TextDocuments_Metadata_IsJson CHECK (Metadata IS NULL OR ISJSON(Metadata) = 1)
);
GO

PRINT 'Created table dbo.TextDocuments';
GO

-- =============================================
-- Table: dbo.ModelMetadata
-- =============================================
-- Represents extended metadata for a model.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.ModelMetadata', 'U') IS NOT NULL
    DROP TABLE dbo.ModelMetadata;
GO

CREATE TABLE dbo.ModelMetadata
(
    MetadataId          INT             IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ModelId             INT             NOT NULL,
    SupportedTasks      NVARCHAR(MAX)   NULL,
    SupportedModalities NVARCHAR(MAX)   NULL,
    MaxInputLength      INT             NULL,
    MaxOutputLength     INT             NULL,
    EmbeddingDimension  INT             NULL,
    PerformanceMetrics  NVARCHAR(MAX)   NULL,
    TrainingDataset     NVARCHAR(500)   NULL,
    TrainingDate        DATE            NULL,
    License             NVARCHAR(100)   NULL,
    SourceUrl           NVARCHAR(500)   NULL,

    CONSTRAINT FK_ModelMetadata_Models FOREIGN KEY (ModelId) REFERENCES dbo.Models(ModelId) ON DELETE CASCADE,
    CONSTRAINT CK_ModelMetadata_SupportedTasks_IsJson CHECK (SupportedTasks IS NULL OR ISJSON(SupportedTasks) = 1),
    CONSTRAINT CK_ModelMetadata_SupportedModalities_IsJson CHECK (SupportedModalities IS NULL OR ISJSON(SupportedModalities) = 1),
    CONSTRAINT CK_ModelMetadata_PerformanceMetrics_IsJson CHECK (PerformanceMetrics IS NULL OR ISJSON(PerformanceMetrics) = 1)
);
GO

CREATE UNIQUE INDEX IX_ModelMetadata_ModelId ON dbo.ModelMetadata(ModelId);
GO

PRINT 'Created table dbo.ModelMetadata';
GO

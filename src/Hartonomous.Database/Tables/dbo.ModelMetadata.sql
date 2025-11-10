-- =============================================
-- Table: dbo.ModelMetadata
-- Description: Extended metadata for models, including capabilities and performance characteristics.
--              One-to-one relationship with Models table.
-- =============================================
CREATE TABLE [dbo].[ModelMetadata]
(
    [MetadataId]           INT              NOT NULL IDENTITY(1,1),
    [ModelId]              INT              NOT NULL,
    [SupportedTasks]       NVARCHAR(MAX)    NULL, -- JSON column type applied via CHECK constraint
    [SupportedModalities]  NVARCHAR(MAX)    NULL, -- JSON column type applied via CHECK constraint
    [MaxInputLength]       INT              NULL,
    [MaxOutputLength]      INT              NULL,
    [EmbeddingDimension]   INT              NULL,
    [PerformanceMetrics]   NVARCHAR(MAX)    NULL, -- JSON column type applied via CHECK constraint
    [TrainingDataset]      NVARCHAR(500)    NULL,
    [TrainingDate]         DATE             NULL,
    [License]              NVARCHAR(100)    NULL,
    [SourceUrl]            NVARCHAR(500)    NULL,

    CONSTRAINT [PK_ModelMetadata] PRIMARY KEY CLUSTERED ([MetadataId] ASC),

    CONSTRAINT [FK_ModelMetadata_Models] 
        FOREIGN KEY ([ModelId]) 
        REFERENCES [dbo].[Models]([ModelId]) 
        ON DELETE CASCADE,

    CONSTRAINT [CK_ModelMetadata_SupportedTasks_IsJson] 
        CHECK ([SupportedTasks] IS NULL OR ISJSON([SupportedTasks]) = 1),

    CONSTRAINT [CK_ModelMetadata_SupportedModalities_IsJson] 
        CHECK ([SupportedModalities] IS NULL OR ISJSON([SupportedModalities]) = 1),

    CONSTRAINT [CK_ModelMetadata_PerformanceMetrics_IsJson] 
        CHECK ([PerformanceMetrics] IS NULL OR ISJSON([PerformanceMetrics]) = 1)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_ModelMetadata_ModelId]
    ON [dbo].[ModelMetadata]([ModelId] ASC);
GO
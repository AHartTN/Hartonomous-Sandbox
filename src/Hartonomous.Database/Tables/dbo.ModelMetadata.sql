CREATE TABLE [dbo].[ModelMetadata] (
    [MetadataId]         INT            NOT NULL IDENTITY,
    [ModelId]            INT            NOT NULL,
    [SupportedTasks]     NVARCHAR(MAX)  NULL,
    [SupportedModalities]NVARCHAR(MAX)  NULL,
    [MaxInputLength]     INT            NULL,
    [MaxOutputLength]    INT            NULL,
    [EmbeddingDimension] INT            NULL,
    [PerformanceMetrics] NVARCHAR(MAX)  NULL,
    [TrainingDataset]    NVARCHAR (500) NULL,
    [TrainingDate]       DATE           NULL,
    [License]            NVARCHAR (100) NULL,
    [SourceUrl]          NVARCHAR (500) NULL,
    CONSTRAINT [PK_ModelMetadata] PRIMARY KEY CLUSTERED ([MetadataId] ASC),
    CONSTRAINT [FK_ModelMetadata_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [dbo].[Models] ([ModelId]) ON DELETE CASCADE
);

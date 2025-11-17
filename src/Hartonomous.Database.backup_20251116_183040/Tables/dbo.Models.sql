-- =============================================
-- Table: dbo.Models
-- Description: Represents AI models ingested into the system.
--              Stores model metadata, configuration, and usage statistics.
-- =============================================
CREATE TABLE [dbo].[Models]
(
    [ModelId]               INT              NOT NULL IDENTITY(1,1),
    [ModelName]             NVARCHAR(200)    NOT NULL,
    [ModelType]             NVARCHAR(100)    NOT NULL,
    [ModelVersion]          NVARCHAR(50)     NULL,
    [Architecture]          NVARCHAR(100)    NULL,
    [Config]                JSON             NULL, -- Native JSON type for structured configuration
    [ParameterCount]        BIGINT           NULL,
    [IngestionDate]         DATETIME2(7)     NOT NULL CONSTRAINT DF_Models_IngestionDate DEFAULT (SYSUTCDATETIME()),
    [CreatedAt]             DATETIME2(7)     NOT NULL CONSTRAINT DF_Models_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [LastUsed]              DATETIME2(7)     NULL,
    [UsageCount]            BIGINT           NOT NULL CONSTRAINT DF_Models_UsageCount DEFAULT (0),
    [AverageInferenceMs]    FLOAT            NULL,
    [TenantId]              INT              NOT NULL CONSTRAINT DF_Models_TenantId DEFAULT (0),
    [IsActive]              BIT              NOT NULL CONSTRAINT DF_Models_IsActive DEFAULT (1),
    [MetadataJson]          JSON             NULL, -- Native JSON type for model metadata
    [SerializedModel]       VARBINARY(MAX)   NULL,

    CONSTRAINT [PK_Models] PRIMARY KEY CLUSTERED ([ModelId] ASC)
);

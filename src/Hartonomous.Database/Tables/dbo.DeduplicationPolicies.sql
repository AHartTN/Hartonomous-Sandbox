-- =============================================
-- Table: dbo.DeduplicationPolicies
-- Description: Configuration for deduplication during ingestion.
--              Defines semantic and spatial thresholds for duplicate detection.
-- =============================================
CREATE TABLE [dbo].[DeduplicationPolicies]
(
    [DeduplicationPolicyId]  INT              NOT NULL IDENTITY(1,1),
    [PolicyName]             NVARCHAR(128)    NOT NULL,
    [SemanticThreshold]      FLOAT            NULL,
    [SpatialThreshold]       FLOAT            NULL,
    [Metadata]               NVARCHAR(MAX)    NULL, -- JSON column type applied via CHECK constraint
    [IsActive]               BIT              NOT NULL DEFAULT (1),
    [CreatedAt]              DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_DeduplicationPolicies] PRIMARY KEY CLUSTERED ([DeduplicationPolicyId] ASC),

    CONSTRAINT [CK_DeduplicationPolicies_Metadata_IsJson] 
        CHECK ([Metadata] IS NULL OR ISJSON([Metadata]) = 1)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_DeduplicationPolicies_PolicyName]
    ON [dbo].[DeduplicationPolicies]([PolicyName] ASC);
GO
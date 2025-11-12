-- =============================================
-- Table: dbo.DeduplicationPolicies
-- =============================================
-- Represents deduplication configuration values for ingestion pipelines.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.DeduplicationPolicies', 'U') IS NOT NULL
    DROP TABLE dbo.DeduplicationPolicies;
GO

CREATE TABLE dbo.DeduplicationPolicies
(
    DeduplicationPolicyId INT             IDENTITY(1,1) NOT NULL PRIMARY KEY,
    PolicyName            NVARCHAR(128)   NOT NULL,
    SemanticThreshold     FLOAT           NULL,
    SpatialThreshold      FLOAT           NULL,
    Metadata              NVARCHAR(MAX)   NULL,
    IsActive              BIT             NOT NULL DEFAULT 1,
    CreatedAt             DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT CK_DeduplicationPolicies_Metadata_IsJson CHECK (Metadata IS NULL OR ISJSON(Metadata) = 1)
);
GO

CREATE UNIQUE INDEX UX_DeduplicationPolicies_PolicyName ON dbo.DeduplicationPolicies(PolicyName);
GO

PRINT 'Created table dbo.DeduplicationPolicies';
GO

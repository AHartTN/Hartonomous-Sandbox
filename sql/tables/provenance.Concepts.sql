-- =============================================
-- Table: provenance.Concepts
-- =============================================
-- Discovered concepts from unsupervised learning.
-- This table was previously managed by EF Core.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'provenance')
BEGIN
    EXEC('CREATE SCHEMA provenance');
END
GO

IF OBJECT_ID('provenance.Concepts', 'U') IS NOT NULL
    DROP TABLE provenance.Concepts;
GO

CREATE TABLE provenance.Concepts
(
    ConceptId           BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ConceptName         NVARCHAR(200)   NOT NULL,
    Description         NVARCHAR(MAX)   NULL,
    CentroidVector      VARBINARY(MAX)  NOT NULL,
    VectorDimension     INT             NOT NULL,
    MemberCount         INT             NOT NULL DEFAULT 0,
    CoherenceScore      REAL            NULL,
    SeparationScore     REAL            NULL,
    DiscoveryMethod     NVARCHAR(100)   NOT NULL,
    ModelId             INT             NOT NULL,
    DiscoveredAt        DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    LastUpdatedAt       DATETIME2       NULL,
    IsActive            BIT             NOT NULL DEFAULT 1,

    CONSTRAINT FK_Concepts_Models FOREIGN KEY (ModelId) REFERENCES dbo.Models(ModelId) ON DELETE CASCADE,
    CONSTRAINT CK_Concepts_Description_IsJson CHECK (Description IS NULL OR ISJSON(Description) = 1) -- Assuming Description can be JSON
);
GO

CREATE INDEX IX_Concepts_ConceptName ON provenance.Concepts(ConceptName);
GO

CREATE INDEX IX_Concepts_ModelId_IsActive ON provenance.Concepts(ModelId, IsActive);
GO

CREATE INDEX IX_Concepts_DiscoveryMethod ON provenance.Concepts(DiscoveryMethod);
GO

CREATE INDEX IX_Concepts_CoherenceScore ON provenance.Concepts(CoherenceScore DESC);
GO

PRINT 'Created table provenance.Concepts';
GO
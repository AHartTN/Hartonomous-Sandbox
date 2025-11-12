-- =============================================
-- Table: dbo.IngestionJobAtoms
-- =============================================
-- Associates an ingestion job with the atoms it produced or referenced.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.IngestionJobAtoms', 'U') IS NOT NULL
    DROP TABLE dbo.IngestionJobAtoms;
GO

CREATE TABLE dbo.IngestionJobAtoms
(
    IngestionJobAtomId  BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IngestionJobId      BIGINT          NOT NULL,
    AtomId              BIGINT          NOT NULL,
    WasDuplicate        BIT             NOT NULL,
    Notes               NVARCHAR(1024)  NULL,

    CONSTRAINT FK_IngestionJobAtoms_IngestionJob FOREIGN KEY (IngestionJobId) REFERENCES dbo.IngestionJobs(IngestionJobId) ON DELETE CASCADE,
    CONSTRAINT FK_IngestionJobAtoms_Atom FOREIGN KEY (AtomId) REFERENCES dbo.Atoms(AtomId) ON DELETE NO ACTION
);
GO

CREATE INDEX IX_IngestionJobAtoms_Job_Atom ON dbo.IngestionJobAtoms(IngestionJobId, AtomId);
GO

PRINT 'Created table dbo.IngestionJobAtoms';
GO

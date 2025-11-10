-- =============================================
-- Table: dbo.IngestionJobAtoms
-- Description: Associates ingestion jobs with the atoms they produced or referenced.
--              Many-to-many link tracking atom creation and reuse during ingestion.
-- =============================================
CREATE TABLE [dbo].[IngestionJobAtoms]
(
    [IngestionJobAtomId]  BIGINT           NOT NULL IDENTITY(1,1),
    [IngestionJobId]      BIGINT           NOT NULL,
    [AtomId]              BIGINT           NOT NULL,
    [WasDuplicate]        BIT              NOT NULL DEFAULT (0),
    [Notes]               NVARCHAR(1024)   NULL,

    CONSTRAINT [PK_IngestionJobAtoms] PRIMARY KEY CLUSTERED ([IngestionJobAtomId] ASC),

    CONSTRAINT [FK_IngestionJobAtoms_IngestionJobs] 
        FOREIGN KEY ([IngestionJobId]) 
        REFERENCES [dbo].[IngestionJobs]([IngestionJobId]) 
        ON DELETE CASCADE,

    CONSTRAINT [FK_IngestionJobAtoms_Atoms] 
        FOREIGN KEY ([AtomId]) 
        REFERENCES [dbo].[Atoms]([AtomId]) 
        ON DELETE NO ACTION
);
GO

CREATE NONCLUSTERED INDEX [IX_IngestionJobAtoms_Job_Atom]
    ON [dbo].[IngestionJobAtoms]([IngestionJobId] ASC, [AtomId] ASC);
GO
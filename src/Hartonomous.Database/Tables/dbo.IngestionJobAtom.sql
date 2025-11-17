CREATE TABLE [dbo].[IngestionJobAtom] (
    [IngestionJobAtomId] BIGINT           NOT NULL IDENTITY,
    [IngestionJobId]     BIGINT           NOT NULL,
    [AtomId]             BIGINT           NOT NULL,
    [WasDuplicate]       BIT              NOT NULL,
    [Notes]              NVARCHAR (1024)  NULL,
    CONSTRAINT [PK_IngestionJobAtoms] PRIMARY KEY CLUSTERED ([IngestionJobAtomId] ASC),
    CONSTRAINT [FK_IngestionJobAtoms_Atoms_AtomId] FOREIGN KEY ([AtomId]) REFERENCES [dbo].[Atom] ([AtomId]),
    CONSTRAINT [FK_IngestionJobAtoms_IngestionJobs_IngestionJobId] FOREIGN KEY ([IngestionJobId]) REFERENCES [dbo].[IngestionJob] ([IngestionJobId]) ON DELETE CASCADE
);

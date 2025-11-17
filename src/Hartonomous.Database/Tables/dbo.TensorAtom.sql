CREATE TABLE [dbo].[TensorAtom] (
    [TensorAtomId]    BIGINT         NOT NULL IDENTITY,
    [AtomId]          BIGINT         NOT NULL,
    [ModelId]         INT            NULL,
    [LayerId]         BIGINT         NULL,
    [AtomType]        NVARCHAR (128) NOT NULL,
    [SpatialSignature]GEOMETRY       NULL,
    [GeometryFootprint]GEOMETRY      NULL,
    [Metadata]        JSON  NULL,
    [ImportanceScore] REAL           NULL,
    [CreatedAt]       DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_TensorAtom] PRIMARY KEY CLUSTERED ([TensorAtomId] ASC),
    CONSTRAINT [FK_TensorAtoms_Atoms_AtomId] FOREIGN KEY ([AtomId]) REFERENCES [dbo].[Atom] ([AtomId]) ON DELETE CASCADE,
    CONSTRAINT [FK_TensorAtoms_ModelLayers_LayerId] FOREIGN KEY ([LayerId]) REFERENCES [dbo].[ModelLayer] ([LayerId]),
    CONSTRAINT [FK_TensorAtoms_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [dbo].[Model] ([ModelId])
);

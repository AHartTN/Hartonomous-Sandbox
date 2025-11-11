CREATE TABLE [dbo].[TensorAtomCoefficients] (
    [TensorAtomCoefficientId] BIGINT         NOT NULL IDENTITY,
    [TensorAtomId]            BIGINT         NOT NULL,
    [ParentLayerId]           BIGINT         NOT NULL,
    [TensorRole]              NVARCHAR (128) NULL,
    [Coefficient]             REAL           NOT NULL,
    CONSTRAINT [PK_TensorAtomCoefficients] PRIMARY KEY CLUSTERED ([TensorAtomCoefficientId] ASC),
    CONSTRAINT [FK_TensorAtomCoefficients_ModelLayers_ParentLayerId] FOREIGN KEY ([ParentLayerId]) REFERENCES [dbo].[ModelLayers] ([LayerId]) ON DELETE CASCADE,
    CONSTRAINT [FK_TensorAtomCoefficients_TensorAtoms_TensorAtomId] FOREIGN KEY ([TensorAtomId]) REFERENCES [dbo].[TensorAtoms] ([TensorAtomId]) ON DELETE CASCADE
);

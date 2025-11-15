CREATE TABLE [dbo].[TensorAtomCoefficients_History] (
    [TensorAtomId]    BIGINT         NOT NULL,
    [ModelId]         INT            NOT NULL,
    [LayerIdx]        INT            NOT NULL,
    [PositionX]       INT            NOT NULL,
    [PositionY]       INT            NOT NULL,
    [PositionZ]       INT            NOT NULL,
    [SpatialKey]      GEOMETRY       NULL,
    [TensorAtomCoefficientId] BIGINT  NULL,
    [ParentLayerId]           BIGINT  NULL,
    [TensorRole]              NVARCHAR(128) NULL,
    [Coefficient]             REAL    NULL,
    [ValidFrom]       DATETIME2(7)   NOT NULL,
    [ValidTo]         DATETIME2(7)   NOT NULL
);
GO

-- Index for temporal queries (cannot use columnstore with GEOMETRY)
CREATE NONCLUSTERED INDEX [IX_TensorAtomCoefficients_History_Period]
    ON [dbo].[TensorAtomCoefficients_History]([ValidTo] ASC, [ValidFrom] ASC);
GO

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
    [ValidTo]         DATETIME2(7)   NOT NULL,
    INDEX [CCI_TensorAtomCoefficients_History] CLUSTERED COLUMNSTORE
);

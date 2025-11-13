CREATE TABLE [dbo].[TensorAtomCoefficients_History] (
    [TensorAtomCoefficientId] BIGINT         NOT NULL,
    [TensorAtomId]            BIGINT         NOT NULL,
    [ParentLayerId]           BIGINT         NOT NULL,
    [TensorRole]              NVARCHAR (128) NULL,
    [Coefficient]             REAL           NOT NULL,
    [ValidFrom]               DATETIME2(7)   NOT NULL,
    [ValidTo]                 DATETIME2(7)   NOT NULL,
    INDEX IX_TensorAtomCoefficients_History_Period NONCLUSTERED ([ValidTo] ASC, [ValidFrom] ASC)
);

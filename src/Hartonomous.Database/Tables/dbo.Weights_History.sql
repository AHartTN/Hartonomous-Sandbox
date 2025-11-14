CREATE TABLE [dbo].[Weights_History] (
    [WeightId]        BIGINT       NOT NULL,
    [LayerID]         BIGINT       NOT NULL,
    [NeuronIndex]     INT          NOT NULL,
    [WeightType]      NVARCHAR(50) NOT NULL,
    [Value]           REAL         NOT NULL,
    [Gradient]        REAL         NULL,
    [Momentum]        REAL         NULL,
    [LastUpdated]     DATETIME2    NOT NULL,
    [UpdateCount]     INT          NOT NULL,
    [ImportanceScore] REAL         NULL,
    [ValidFrom]       DATETIME2(7) NOT NULL,
    [ValidTo]         DATETIME2(7) NOT NULL,
    INDEX CCI_Weights_History CLUSTERED COLUMNSTORE
);

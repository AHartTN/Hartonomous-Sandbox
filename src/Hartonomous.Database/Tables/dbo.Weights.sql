CREATE TABLE [dbo].[Weights]
(
    [WeightId] BIGINT IDENTITY(1,1) NOT NULL,
    [LayerID] BIGINT NOT NULL,
    [NeuronIndex] INT NOT NULL,
    [WeightType] NVARCHAR(50) NOT NULL DEFAULT ('parameter'),
    [Value] REAL NOT NULL,
    [Gradient] REAL NULL,
    [Momentum] REAL NULL,
    [LastUpdated] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdateCount] INT NOT NULL DEFAULT (0),
    [ImportanceScore] REAL NULL DEFAULT (0.5),
    
    CONSTRAINT [PK_Weights] PRIMARY KEY CLUSTERED ([WeightId]),
    CONSTRAINT [FK_Weights_Layers] FOREIGN KEY ([LayerID])
        REFERENCES [dbo].[ModelLayers]([LayerId]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_Weights_Layer]
    ON [dbo].[Weights]([LayerID], [NeuronIndex]);
GO

CREATE NONCLUSTERED INDEX [IX_Weights_Importance]
    ON [dbo].[Weights]([ImportanceScore] DESC)
    WHERE [ImportanceScore] > 0.7;
GO
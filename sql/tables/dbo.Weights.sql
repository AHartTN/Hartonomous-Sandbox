USE Hartonomous;
GO

IF OBJECT_ID(N'dbo.Weights', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Weights (
        WeightId BIGINT IDENTITY(1,1) NOT NULL,
        LayerID INT NOT NULL,
        NeuronIndex INT NOT NULL,
        WeightType NVARCHAR(50) NOT NULL DEFAULT 'parameter',
        Value REAL NOT NULL,
        Gradient REAL NULL,
        Momentum REAL NULL,
        LastUpdated DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdateCount INT NOT NULL DEFAULT 0,
        ImportanceScore REAL NULL DEFAULT 0.5,

        CONSTRAINT PK_Weights PRIMARY KEY CLUSTERED (WeightId),
        CONSTRAINT FK_Weights_Layers FOREIGN KEY (LayerID)
            REFERENCES dbo.ModelLayers(LayerId) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Weights_Layer' AND object_id = OBJECT_ID(N'dbo.Weights'))
BEGIN
    CREATE INDEX IX_Weights_Layer ON dbo.Weights (LayerID, NeuronIndex);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Weights_Importance' AND object_id = OBJECT_ID(N'dbo.Weights'))
BEGIN
    CREATE INDEX IX_Weights_Importance ON dbo.Weights (ImportanceScore DESC) WHERE ImportanceScore > 0.7;
END
GO

IF COL_LENGTH('dbo.Weights', 'ValidFrom') IS NULL
BEGIN
    ALTER TABLE dbo.Weights
    ADD
        ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START NOT NULL DEFAULT SYSUTCDATETIME(),
        ValidTo DATETIME2 GENERATED ALWAYS AS ROW END NOT NULL DEFAULT CAST('9999-12-31 23:59:59.9999999' AS DATETIME2),
        PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Weights' AND temporal_type = 2)
BEGIN
    ALTER TABLE dbo.Weights
    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.Weights_History));
END
GO

PRINT 'Created Weights table with temporal versioning';

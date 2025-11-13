CREATE TABLE dbo.Weights (
    WeightId BIGINT IDENTITY(1,1) NOT NULL,
    LayerID BIGINT NOT NULL,  -- Fixed: Changed from INT to BIGINT to match ModelLayers.LayerId
    NeuronIndex INT NOT NULL,
    WeightType NVARCHAR(50) NOT NULL DEFAULT 'parameter',
    Value REAL NOT NULL,
    Gradient REAL NULL,
    Momentum REAL NULL,
    LastUpdated DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdateCount INT NOT NULL DEFAULT 0,
    ImportanceScore REAL NULL DEFAULT 0.5,
    ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START NOT NULL DEFAULT SYSUTCDATETIME(),
    ValidTo DATETIME2 GENERATED ALWAYS AS ROW END NOT NULL DEFAULT CAST('9999-12-31 23:59:59.9999999' AS DATETIME2),
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo),
    CONSTRAINT PK_Weights PRIMARY KEY CLUSTERED (WeightId),
    CONSTRAINT FK_Weights_Layers FOREIGN KEY (LayerID) REFERENCES dbo.ModelLayers(LayerId) ON DELETE CASCADE,
    INDEX IX_Weights_Layer (LayerID, NeuronIndex),
    INDEX IX_Weights_Importance (ImportanceScore DESC) WHERE (ImportanceScore > 0.7)
) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.Weights_History));
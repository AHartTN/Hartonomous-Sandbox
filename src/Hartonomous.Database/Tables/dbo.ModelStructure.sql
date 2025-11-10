USE Hartonomous;
GO

IF OBJECT_ID(N'dbo.Models', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Models (
        ModelId INT IDENTITY(1,1) NOT NULL,
        ModelName NVARCHAR(256) NOT NULL,
        ModelType NVARCHAR(100) NULL,
        Description NVARCHAR(MAX) NULL,
        MetadataJson NVARCHAR(MAX) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        IsPublic BIT NOT NULL DEFAULT 0,
        CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Models PRIMARY KEY CLUSTERED (ModelId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Models_Name' AND object_id = OBJECT_ID(N'dbo.Models'))
BEGIN
    CREATE INDEX IX_Models_Name ON dbo.Models (ModelName);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Models_Active' AND object_id = OBJECT_ID(N'dbo.Models'))
BEGIN
    CREATE INDEX IX_Models_Active ON dbo.Models (IsActive) WHERE IsActive = 1;
END
GO

IF OBJECT_ID(N'dbo.ModelLayers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ModelLayers (
        LayerId INT IDENTITY(1,1) NOT NULL,
        ModelId INT NOT NULL,
        LayerName NVARCHAR(100) NOT NULL,
        LayerType NVARCHAR(50) NOT NULL,
        LayerIndex INT NOT NULL,
        NeuronCount INT NOT NULL,
        ActivationFunction NVARCHAR(50) NULL,
        ConfigJson NVARCHAR(MAX) NULL,

        CONSTRAINT PK_ModelLayers PRIMARY KEY CLUSTERED (LayerId),
        CONSTRAINT FK_ModelLayers_Models FOREIGN KEY (ModelId)
            REFERENCES dbo.Models(ModelId) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ModelLayers_Model' AND object_id = OBJECT_ID(N'dbo.ModelLayers'))
BEGIN
    CREATE INDEX IX_ModelLayers_Model ON dbo.ModelLayers (ModelId, LayerIndex);
END
GO

PRINT 'Created Models and ModelLayers tables';

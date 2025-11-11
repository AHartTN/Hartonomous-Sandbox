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
    CONSTRAINT PK_Models PRIMARY KEY CLUSTERED (ModelId),
    INDEX IX_Models_Name (ModelName),
    INDEX IX_Models_Active (IsActive) WHERE (IsActive = 1)
);

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
    CONSTRAINT FK_ModelLayers_Models FOREIGN KEY (ModelId) REFERENCES dbo.Models(ModelId) ON DELETE CASCADE,
    INDEX IX_ModelLayers_Model (ModelId, LayerIndex)
);
-- =============================================
-- Table: dbo.LayerTensorSegments
-- =============================================
-- Represents a persisted tensor segment for a model layer.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.LayerTensorSegments', 'U') IS NOT NULL
    DROP TABLE dbo.LayerTensorSegments;
GO

CREATE TABLE dbo.LayerTensorSegments
(
    LayerTensorSegmentId BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    LayerId              BIGINT          NOT NULL,
    SegmentOrdinal       INT             NOT NULL,
    PointOffset          BIGINT          NOT NULL,
    PointCount           INT             NOT NULL,
    QuantizationType     NVARCHAR(20)    NOT NULL,
    QuantizationScale    FLOAT           NULL,
    QuantizationZeroPoint FLOAT          NULL,
    ZMin                 FLOAT           NULL,
    ZMax                 FLOAT           NULL,
    MMin                 FLOAT           NULL,
    MMax                 FLOAT           NULL,
    MortonCode           BIGINT          NULL,
    GeometryFootprint    GEOMETRY        NULL,
    RawPayload           VARBINARY(MAX)  FILESTREAM NOT NULL,
    PayloadRowGuid       UNIQUEIDENTIFIER ROWGUIDCOL NOT NULL UNIQUE DEFAULT NEWSEQUENTIALID(),
    CreatedAt            DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_LayerTensorSegments_ModelLayers FOREIGN KEY (LayerId) REFERENCES dbo.ModelLayers(LayerId) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX UX_LayerTensorSegments_LayerId_SegmentOrdinal ON dbo.LayerTensorSegments(LayerId, SegmentOrdinal);
GO

CREATE INDEX IX_LayerTensorSegments_Z_Range ON dbo.LayerTensorSegments(LayerId, ZMin, ZMax);
GO

CREATE INDEX IX_LayerTensorSegments_M_Range ON dbo.LayerTensorSegments(LayerId, MMin, MMax);
GO

CREATE INDEX IX_LayerTensorSegments_Morton ON dbo.LayerTensorSegments(MortonCode);
GO

PRINT 'Created table dbo.LayerTensorSegments';
GO

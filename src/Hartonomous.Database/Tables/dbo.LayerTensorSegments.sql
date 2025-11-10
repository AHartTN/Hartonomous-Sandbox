-- =============================================
-- Table: dbo.LayerTensorSegments
-- Description: Persisted tensor segments for model layers using FILESTREAM.
--              Enables massive tensor storage with spatial metadata for fast filtering.
-- =============================================
CREATE TABLE [dbo].[LayerTensorSegments]
(
    [LayerTensorSegmentId]   BIGINT              NOT NULL IDENTITY(1,1),
    [LayerId]                BIGINT              NOT NULL,
    [SegmentOrdinal]         INT                 NOT NULL,
    [PointOffset]            BIGINT              NOT NULL,
    [PointCount]             INT                 NOT NULL,
    [QuantizationType]       NVARCHAR(20)        NOT NULL,
    [QuantizationScale]      FLOAT               NULL,
    [QuantizationZeroPoint]  FLOAT               NULL,
    [ZMin]                   FLOAT               NULL,
    [ZMax]                   FLOAT               NULL,
    [MMin]                   FLOAT               NULL,
    [MMax]                   FLOAT               NULL,
    [MortonCode]             BIGINT              NULL,
    [GeometryFootprint]      GEOMETRY            NULL,
    [RawPayload]             VARBINARY(MAX) FILESTREAM NOT NULL,
    [PayloadRowGuid]         UNIQUEIDENTIFIER ROWGUIDCOL NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [CreatedAt]              DATETIME2(7)        NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_LayerTensorSegments] PRIMARY KEY CLUSTERED ([LayerTensorSegmentId] ASC),

    CONSTRAINT [UX_LayerTensorSegments_PayloadRowGuid] UNIQUE ([PayloadRowGuid]),

    CONSTRAINT [FK_LayerTensorSegments_ModelLayers] 
        FOREIGN KEY ([LayerId]) 
        REFERENCES [dbo].[ModelLayers]([LayerId]) 
        ON DELETE CASCADE
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_LayerTensorSegments_LayerId_SegmentOrdinal]
    ON [dbo].[LayerTensorSegments]([LayerId] ASC, [SegmentOrdinal] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_LayerTensorSegments_Z_Range]
    ON [dbo].[LayerTensorSegments]([LayerId] ASC, [ZMin] ASC, [ZMax] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_LayerTensorSegments_M_Range]
    ON [dbo].[LayerTensorSegments]([LayerId] ASC, [MMin] ASC, [MMax] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_LayerTensorSegments_Morton]
    ON [dbo].[LayerTensorSegments]([MortonCode] ASC);
GO
-- =============================================
-- Table: dbo.CachedActivations
-- Description: Cached layer activations to accelerate inference by avoiding redundant computation.
--              Uses VECTOR type for activation outputs with LRU eviction tracking.
-- =============================================
CREATE TABLE [dbo].[CachedActivations]
(
    [CacheId]              BIGINT           NOT NULL IDENTITY(1,1),
    [ModelId]              INT              NOT NULL,
    [LayerId]              BIGINT           NOT NULL,
    [InputHash]            BINARY(32)       NOT NULL,
    [ActivationOutput]     VECTOR(1998)     NULL, -- Max dimension for VECTOR type
    [OutputShape]          NVARCHAR(100)    NULL,
    [HitCount]             BIGINT           NOT NULL DEFAULT (0),
    [CreatedDate]          DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastAccessed]         DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
    [ComputeTimeSavedMs]   BIGINT           NOT NULL DEFAULT (0),

    CONSTRAINT [PK_CachedActivations] PRIMARY KEY CLUSTERED ([CacheId] ASC),

    CONSTRAINT [FK_CachedActivations_Models] 
        FOREIGN KEY ([ModelId]) 
        REFERENCES [dbo].[Models]([ModelId]) 
        ON DELETE NO ACTION,

    CONSTRAINT [FK_CachedActivations_ModelLayers] 
        FOREIGN KEY ([LayerId]) 
        REFERENCES [dbo].[ModelLayers]([LayerId]) 
        ON DELETE CASCADE
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_CachedActivations_Model_Layer_InputHash]
    ON [dbo].[CachedActivations]([ModelId] ASC, [LayerId] ASC, [InputHash] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_CachedActivations_LastAccessed_HitCount]
    ON [dbo].[CachedActivations]([LastAccessed] DESC, [HitCount] DESC);
GO
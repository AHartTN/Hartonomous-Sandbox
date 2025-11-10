-- =============================================
-- Table: dbo.SessionPaths
-- =============================================
-- Stores user or system interaction trajectories as geometric paths.
-- Each path is a LineString where each point represents an interaction
-- (an Atom) in space and time. The M-coordinate of each point in the
-- LineString stores the timestamp of the interaction.
-- =============================================
CREATE TABLE [dbo].[SessionPaths]
(
    [SessionPathId] BIGINT IDENTITY(1,1) NOT NULL,
    [SessionId] UNIQUEIDENTIFIER NOT NULL,
    [Path] GEOMETRY NOT NULL,
    [PathLength] AS ([Path].STLength()),
    [StartTime] AS ([Path].STPointN(1).M),
    [EndTime] AS ([Path].STPointN([Path].STNumPoints()).M),
    [TenantId] INT NOT NULL DEFAULT (0),
    [CreatedAt] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_SessionPaths] PRIMARY KEY CLUSTERED ([SessionPathId])
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_SessionPaths_SessionId]
    ON [dbo].[SessionPaths]([SessionId]);
GO

CREATE SPATIAL INDEX [IX_SessionPaths_Path]
    ON [dbo].[SessionPaths]([Path])
    WITH (GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM));
GO
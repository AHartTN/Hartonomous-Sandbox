USE Hartonomous;
GO

-- Drop the table if it already exists
IF OBJECT_ID('dbo.SessionPaths', 'U') IS NOT NULL
    DROP TABLE dbo.SessionPaths;
GO

-- =============================================
-- Table: dbo.SessionPaths
-- =============================================
-- Stores user or system interaction trajectories as geometric paths.
-- Each path is a LineString where each point represents an interaction
-- (an Atom) in space and time. The M-coordinate of each point in the
-- LineString stores the timestamp of the interaction.
-- =============================================
CREATE TABLE dbo.SessionPaths (
    SessionPathId BIGINT IDENTITY(1,1) NOT NULL,
    SessionId UNIQUEIDENTIFIER NOT NULL,
    Path GEOMETRY NOT NULL,
    PathLength AS Path.STLength(), -- Computed column for path length
    StartTime AS Path.STPointN(1).M, -- Computed column for the M-value (timestamp) of the first point
    EndTime AS Path.STPointN(Path.STNumPoints()).M, -- Computed column for the M-value of the last point
    TenantId INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_SessionPaths PRIMARY KEY CLUSTERED (SessionPathId),
    INDEX IX_SessionPaths_SessionId UNIQUE NONCLUSTERED (SessionId)
);
GO

-- Create a spatial index on the Path column for efficient geometric queries
-- (e.g., finding paths that intersect a certain region).
CREATE SPATIAL INDEX IX_SessionPaths_Path
    ON dbo.SessionPaths(Path)
    WITH (GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM));
GO

PRINT 'Created table dbo.SessionPaths with spatial index.';
GO

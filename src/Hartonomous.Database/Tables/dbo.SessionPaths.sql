CREATE TABLE dbo.SessionPaths (
    SessionPathId BIGINT IDENTITY(1,1) NOT NULL,
    SessionId UNIQUEIDENTIFIER NOT NULL,
    Path GEOMETRY NOT NULL,
    PathLength AS (Path.STLength()),
    StartTime AS (Path.STPointN(1).M),
    EndTime AS (Path.STPointN(Path.STNumPoints()).M),
    TenantId INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_SessionPaths PRIMARY KEY CLUSTERED (SessionPathId),
    INDEX IX_SessionPaths_SessionId UNIQUE (SessionId)
);
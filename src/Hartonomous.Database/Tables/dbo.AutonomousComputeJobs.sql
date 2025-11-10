USE Hartonomous;
GO

-- AutonomousComputeJobs: Persistent state management for long-running autonomous compute tasks
-- Supports the OODA loop's ability to work on abstract, multi-chunk problems (e.g., prime search, proof search)
-- Each job is processed incrementally via the Analyze -> Hypothesize -> Act -> Learn cycle

CREATE TABLE dbo.AutonomousComputeJobs
(
    JobId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    JobType NVARCHAR(100) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
    JobParameters NVARCHAR(MAX) NOT NULL,
    CurrentState NVARCHAR(MAX) NULL,
    Results NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CompletedAt DATETIME2 NULL,
    
    CONSTRAINT CK_AutonomousComputeJobs_Status 
        CHECK (Status IN ('Pending', 'Running', 'Completed', 'Failed', 'Cancelled')),
    
    CONSTRAINT CK_AutonomousComputeJobs_JobParameters_IsJson
        CHECK (ISJSON(JobParameters) = 1),
    
    CONSTRAINT CK_AutonomousComputeJobs_CurrentState_IsJson
        CHECK (CurrentState IS NULL OR ISJSON(CurrentState) = 1),
    
    CONSTRAINT CK_AutonomousComputeJobs_Results_IsJson
        CHECK (Results IS NULL OR ISJSON(Results) = 1)
);
GO

-- Index for active job queries
CREATE NONCLUSTERED INDEX IX_AutonomousComputeJobs_Status_CreatedAt
    ON dbo.AutonomousComputeJobs(Status, CreatedAt)
    INCLUDE (JobId, JobType);
GO

-- Index for job type queries
CREATE NONCLUSTERED INDEX IX_AutonomousComputeJobs_JobType
    ON dbo.AutonomousComputeJobs(JobType)
    INCLUDE (Status, CreatedAt);
GO

PRINT 'Created table dbo.AutonomousComputeJobs with indexes.';
GO

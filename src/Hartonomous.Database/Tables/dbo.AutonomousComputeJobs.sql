CREATE TABLE dbo.AutonomousComputeJobs (
    JobId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    JobType NVARCHAR(100) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
    JobParameters JSON NOT NULL, -- Native JSON for job configuration
    CurrentState JSON NULL, -- Native JSON for job state tracking
    Results JSON NULL, -- Native JSON for computation results
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    LastHeartbeat DATETIME2 NULL, -- Track when job last reported progress
    CompletedAt DATETIME2 NULL,
    CONSTRAINT PK_AutonomousComputeJobs PRIMARY KEY (JobId),
    CONSTRAINT CK_AutonomousComputeJobs_Status CHECK (Status IN ('Pending', 'Running', 'Completed', 'Failed', 'Cancelled')),
    INDEX IX_AutonomousComputeJobs_Status_CreatedAt (Status, CreatedAt) INCLUDE (JobId, JobType),
    INDEX IX_AutonomousComputeJobs_JobType (JobType) INCLUDE (Status, CreatedAt)
);
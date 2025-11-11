CREATE TABLE dbo.AutonomousComputeJobs (
    JobId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    JobType NVARCHAR(100) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
    JobParameters NVARCHAR(MAX) NOT NULL,
    CurrentState NVARCHAR(MAX) NULL,
    Results NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CompletedAt DATETIME2 NULL,
    CONSTRAINT PK_AutonomousComputeJobs PRIMARY KEY (JobId),
    CONSTRAINT CK_AutonomousComputeJobs_Status CHECK (Status IN ('Pending', 'Running', 'Completed', 'Failed', 'Cancelled')),
    CONSTRAINT CK_AutonomousComputeJobs_JobParameters_IsJson CHECK (ISJSON(JobParameters) = 1),
    CONSTRAINT CK_AutonomousComputeJobs_CurrentState_IsJson CHECK (CurrentState IS NULL OR ISJSON(CurrentState) = 1),
    CONSTRAINT CK_AutonomousComputeJobs_Results_IsJson CHECK (Results IS NULL OR ISJSON(Results) = 1),
    INDEX IX_AutonomousComputeJobs_Status_CreatedAt (Status, CreatedAt) INCLUDE (JobId, JobType),
    INDEX IX_AutonomousComputeJobs_JobType (JobType) INCLUDE (Status, CreatedAt)
);
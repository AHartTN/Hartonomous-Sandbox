CREATE TABLE [dbo].[TestResult] (
    [TestResultId]    BIGINT         NOT NULL IDENTITY,
    [TestName]        NVARCHAR (200) NOT NULL,
    [TestSuite]       NVARCHAR (100) NOT NULL,
    [TestStatus]      NVARCHAR (50)  NOT NULL,
    [ExecutionTimeMs] FLOAT (53)     NULL,
    [ErrorMessage]    NVARCHAR (MAX) NULL,
    [StackTrace]      NVARCHAR (MAX) NULL,
    [TestOutput]      NVARCHAR (MAX) NULL,
    [ExecutedAt]      DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    [Environment]     NVARCHAR (100) NULL,
    [TestCategory]    NVARCHAR (50)  NULL,
    [MemoryUsageMB]   FLOAT (53)     NULL,
    [CpuUsagePercent] FLOAT (53)     NULL,
    CONSTRAINT [PK_TestResults] PRIMARY KEY CLUSTERED ([TestResultId] ASC)
);

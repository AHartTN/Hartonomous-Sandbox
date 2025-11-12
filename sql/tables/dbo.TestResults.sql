-- =============================================
-- Table: dbo.TestResults
-- =============================================
-- Stores test execution results and performance metrics.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.TestResults', 'U') IS NOT NULL
    DROP TABLE dbo.TestResults;
GO

CREATE TABLE dbo.TestResults
(
    TestResultId        BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TestName            NVARCHAR(200)   NOT NULL,
    TestSuite           NVARCHAR(100)   NOT NULL,
    TestStatus          NVARCHAR(50)    NOT NULL,
    ExecutionTimeMs     REAL            NULL,
    ErrorMessage        NVARCHAR(MAX)   NULL,
    StackTrace          NVARCHAR(MAX)   NULL,
    TestOutput          NVARCHAR(MAX)   NULL,
    ExecutedAt          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    Environment         NVARCHAR(100)   NULL,
    TestCategory        NVARCHAR(50)    NULL,
    MemoryUsageMB       REAL            NULL,
    CpuUsagePercent     REAL            NULL
);
GO

CREATE INDEX IX_TestResults_TestSuite_ExecutedAt ON dbo.TestResults(TestSuite, ExecutedAt DESC);
GO

CREATE INDEX IX_TestResults_TestStatus ON dbo.TestResults(TestStatus);
GO

CREATE INDEX IX_TestResults_TestCategory_ExecutedAt ON dbo.TestResults(TestCategory, ExecutedAt DESC);
GO

CREATE INDEX IX_TestResults_ExecutionTimeMs ON dbo.TestResults(ExecutionTimeMs DESC);
GO

PRINT 'Created table dbo.TestResults';
GO
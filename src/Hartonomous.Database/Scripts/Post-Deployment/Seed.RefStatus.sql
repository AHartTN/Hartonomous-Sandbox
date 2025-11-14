/*
Post-Deployment Script: Seed ref.Status
Populates reference status codes with temporal tracking
*/

PRINT 'Seeding ref.Status...';

-- Use MERGE for idempotent seeding (system versioning handles history automatically)
MERGE INTO [ref].[Status] AS target
USING (
    VALUES
        ('PENDING',     'Pending',     'Operation is queued and awaiting execution', 10),
        ('RUNNING',     'Running',     'Operation is currently executing', 20),
        ('COMPLETED',   'Completed',   'Operation finished successfully', 30),
        ('FAILED',      'Failed',      'Operation encountered an error', 40),
        ('CANCELLED',   'Cancelled',   'Operation was cancelled by user or system', 50),
        ('EXECUTED',    'Executed',    'Command or task was executed', 60),
        ('HIGH_SUCCESS','High Success','Operation completed with high confidence/quality', 70),
        ('SUCCESS',     'Success',     'Operation completed successfully', 80),
        ('REGRESSED',   'Regressed',   'Performance or quality decreased from baseline', 90),
        ('WARN',        'Warning',     'Operation completed with warnings', 100)
) AS source ([Code], [Name], [Description], [SortOrder])
ON target.[Code] = source.[Code]
WHEN MATCHED THEN
    UPDATE SET
        [Name] = source.[Name],
        [Description] = source.[Description],
        [SortOrder] = source.[SortOrder],
        [UpdatedAt] = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT ([Code], [Name], [Description], [SortOrder])
    VALUES (source.[Code], source.[Name], source.[Description], source.[SortOrder]);

DECLARE @rowsAffected INT = @@ROWCOUNT;

PRINT '  ✓ Seeded ' + CAST(@rowsAffected AS VARCHAR) + ' status codes into ref.Status';

-- Verify seeding
DECLARE @statusCount INT = (SELECT COUNT(*) FROM [ref].[Status] WHERE [IsActive] = 1);
IF @statusCount < 10
BEGIN
    RAISERROR('ref.Status seeding incomplete: expected 10+ status codes, found %d', 16, 1, @statusCount);
END
ELSE
BEGIN
    PRINT '  ✓ Validation passed: ' + CAST(@statusCount AS VARCHAR) + ' active status codes';
END;

GO

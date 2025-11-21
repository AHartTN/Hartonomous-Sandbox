/*
 * SQL Server Agent Job: Hartonomous Cognitive Kernel (OODA Loop)
 * Purpose: Provision autonomous self-optimization agent for production deployment
 * 
 * This script creates a SQL Server Agent Job that executes the OODA 
 * (Observe, Orient, Decide, Act) cycle at regular intervals, enabling 
 * the Hartonomous system to autonomously:
 * 
 * - Observe: Monitor system performance, query patterns, and resource usage
 * - Orient: Analyze trends and identify optimization opportunities
 * - Decide: Generate actionable hypotheses for system improvements
 * - Act: Execute approved optimizations (indexing, pruning, rebalancing)
 * 
 * Prerequisites:
 * - SQL Server Agent service running
 * - dbo.sp_Analyze stored procedure deployed
 * - Appropriate permissions for autonomous operations
 * 
 * Author: Hartonomous Engineering Team
 * Date: 2025-11-20
 * Version: 1.0
 */

USE msdb;
GO

SET NOCOUNT ON;
GO

PRINT '========================================================';
PRINT 'Provisioning Hartonomous Cognitive Kernel Agent Job';
PRINT 'Timestamp: ' + CONVERT(VARCHAR(23), GETDATE(), 121);
PRINT '========================================================';
PRINT '';
GO

-- ============================================================================
-- Configuration Variables
-- ============================================================================

DECLARE @JobName NVARCHAR(128) = N'Hartonomous_Cognitive_Kernel';
DECLARE @JobDescription NVARCHAR(512) = N'Autonomous OODA Loop: Observe, Orient, Decide, Act cycle for self-optimization';
DECLARE @DatabaseName NVARCHAR(128) = N'Hartonomous';
DECLARE @ScheduleName NVARCHAR(128) = N'Every 15 Minutes';
DECLARE @CategoryName NVARCHAR(128) = N'Hartonomous Automation';
DECLARE @OwnerLoginName NVARCHAR(128) = SUSER_SNAME(); -- Current user

-- ============================================================================
-- Step 1: Create Job Category (if not exists)
-- ============================================================================

IF NOT EXISTS (
    SELECT 1 
    FROM msdb.dbo.syscategories 
    WHERE name = @CategoryName 
      AND category_class = 1 -- Job category
)
BEGIN
    PRINT 'Creating job category: ' + @CategoryName;
    
    EXEC msdb.dbo.sp_add_category
        @class = N'JOB',
        @type = N'LOCAL',
        @name = @CategoryName;
    
    PRINT 'Job category created successfully.';
    PRINT '';
END
ELSE
BEGIN
    PRINT 'Job category already exists: ' + @CategoryName;
    PRINT '';
END
GO

-- ============================================================================
-- Step 2: Delete Existing Job (if exists)
-- ============================================================================

DECLARE @JobName NVARCHAR(128) = N'Hartonomous_Cognitive_Kernel';
DECLARE @JobId UNIQUEIDENTIFIER;

SELECT @JobId = job_id 
FROM msdb.dbo.sysjobs 
WHERE name = @JobName;

IF @JobId IS NOT NULL
BEGIN
    PRINT 'Deleting existing job: ' + @JobName;
    
    EXEC msdb.dbo.sp_delete_job 
        @job_id = @JobId, 
        @delete_unused_schedule = 1;
    
    PRINT 'Existing job deleted.';
    PRINT '';
END
ELSE
BEGIN
    PRINT 'No existing job found.';
    PRINT '';
END
GO

-- ============================================================================
-- Step 3: Create New Job
-- ============================================================================

DECLARE @JobName NVARCHAR(128) = N'Hartonomous_Cognitive_Kernel';
DECLARE @JobDescription NVARCHAR(512) = N'Autonomous OODA Loop: Observe, Orient, Decide, Act cycle for self-optimization';
DECLARE @CategoryName NVARCHAR(128) = N'Hartonomous Automation';
DECLARE @OwnerLoginName NVARCHAR(128) = SUSER_SNAME();
DECLARE @ReturnCode INT;
DECLARE @JobId UNIQUEIDENTIFIER;

BEGIN TRANSACTION;

PRINT 'Creating job: ' + @JobName;

EXEC @ReturnCode = msdb.dbo.sp_add_job
    @job_name = @JobName,
    @enabled = 1,
    @notify_level_eventlog = 2, -- Log on failure
    @notify_level_email = 0,
    @notify_level_netsend = 0,
    @notify_level_page = 0,
    @delete_level = 0,
    @description = @JobDescription,
    @category_name = @CategoryName,
    @owner_login_name = @OwnerLoginName,
    @job_id = @JobId OUTPUT;

IF @ReturnCode <> 0 
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR('Failed to create job', 16, 1);
END
ELSE
BEGIN
    PRINT 'Job created with ID: ' + CAST(@JobId AS NVARCHAR(50));
    PRINT '';
END

-- ============================================================================
-- Step 4: Add Job Step - Execute OODA Loop
-- ============================================================================

DECLARE @DatabaseName NVARCHAR(128) = N'Hartonomous';
DECLARE @StepCommand NVARCHAR(MAX) = N'
-- Hartonomous Cognitive Kernel: OODA Loop Execution
-- Observe, Orient, Decide, Act

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @startTime DATETIME2 = SYSUTCDATETIME();
DECLARE @analysisId UNIQUEIDENTIFIER = NEWID();

PRINT ''========================================'';
PRINT ''Hartonomous OODA Loop Execution'';
PRINT ''Analysis ID: '' + CAST(@analysisId AS NVARCHAR(50));
PRINT ''Timestamp: '' + CONVERT(VARCHAR(23), @startTime, 121);
PRINT ''========================================'';
PRINT '''';

BEGIN TRY
    -- Execute autonomous analysis stored procedure
    EXEC dbo.sp_Analyze;
    
    DECLARE @durationMs INT = DATEDIFF(MILLISECOND, @startTime, SYSUTCDATETIME());
    
    PRINT '''';
    PRINT ''OODA Loop completed successfully'';
    PRINT ''Duration: '' + CAST(@durationMs AS NVARCHAR(20)) + '' ms'';
    PRINT ''========================================'';
    
END TRY
BEGIN CATCH
    DECLARE @errorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @errorSeverity INT = ERROR_SEVERITY();
    DECLARE @errorState INT = ERROR_STATE();
    
    PRINT '''';
    PRINT ''ERROR: OODA Loop execution failed'';
    PRINT ''Message: '' + @errorMessage;
    PRINT ''Severity: '' + CAST(@errorSeverity AS NVARCHAR(10));
    PRINT ''State: '' + CAST(@errorState AS NVARCHAR(10));
    PRINT ''========================================'';
    
    -- Log error but do not fail the job (retry on next cycle)
    -- RAISERROR(@errorMessage, @errorSeverity, @errorState);
END CATCH
';

PRINT 'Adding job step: Execute OODA Loop';

EXEC @ReturnCode = msdb.dbo.sp_add_jobstep
    @job_id = @JobId,
    @step_name = N'Execute OODA Loop',
    @step_id = 1,
    @cmdexec_success_code = 0,
    @on_success_action = 1, -- Quit with success
    @on_fail_action = 2,    -- Quit with failure
    @retry_attempts = 0,
    @retry_interval = 0,
    @os_run_priority = 0,
    @subsystem = N'TSQL',
    @command = @StepCommand,
    @database_name = @DatabaseName,
    @flags = 0;

IF @ReturnCode <> 0 
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR('Failed to add job step', 16, 1);
END
ELSE
BEGIN
    PRINT 'Job step added successfully.';
    PRINT '';
END

-- ============================================================================
-- Step 5: Set Job Start Step
-- ============================================================================

PRINT 'Setting job start step...';

EXEC @ReturnCode = msdb.dbo.sp_update_job
    @job_id = @JobId,
    @start_step_id = 1;

IF @ReturnCode <> 0 
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR('Failed to update job start step', 16, 1);
END

PRINT 'Job start step configured.';
PRINT '';

-- ============================================================================
-- Step 6: Create Schedule - Every 15 Minutes
-- ============================================================================

DECLARE @ScheduleName NVARCHAR(128) = N'Every 15 Minutes';
DECLARE @ScheduleId INT;

PRINT 'Creating job schedule: ' + @ScheduleName;

EXEC @ReturnCode = msdb.dbo.sp_add_jobschedule
    @job_id = @JobId,
    @name = @ScheduleName,
    @enabled = 1,
    @freq_type = 4,              -- Daily
    @freq_interval = 1,          -- Every 1 day
    @freq_subday_type = 4,       -- Minutes
    @freq_subday_interval = 15,  -- Every 15 minutes
    @freq_relative_interval = 0,
    @freq_recurrence_factor = 0,
    @active_start_date = 20250101, -- Start from January 1, 2025
    @active_end_date = 99991231,   -- No end date
    @active_start_time = 0,        -- Midnight
    @active_end_time = 235959,     -- 11:59:59 PM
    @schedule_id = @ScheduleId OUTPUT;

IF @ReturnCode <> 0 
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR('Failed to create job schedule', 16, 1);
END
ELSE
BEGIN
    PRINT 'Job schedule created with ID: ' + CAST(@ScheduleId AS NVARCHAR(10));
    PRINT '';
END

-- ============================================================================
-- Step 7: Assign Job to Local Server
-- ============================================================================

PRINT 'Assigning job to local server...';

EXEC @ReturnCode = msdb.dbo.sp_add_jobserver
    @job_id = @JobId,
    @server_name = N'(local)';

IF @ReturnCode <> 0 
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR('Failed to assign job to server', 16, 1);
END

PRINT 'Job assigned to server successfully.';
PRINT '';

COMMIT TRANSACTION;

-- ============================================================================
-- Step 8: Verification
-- ============================================================================

PRINT '========================================================';
PRINT 'Verification';
PRINT '========================================================';
PRINT '';

-- Display job details
SELECT 
    j.name AS JobName,
    j.enabled AS IsEnabled,
    j.description AS Description,
    c.name AS Category,
    SUSER_SNAME(j.owner_sid) AS Owner,
    j.date_created AS DateCreated,
    j.date_modified AS DateModified
FROM msdb.dbo.sysjobs j
INNER JOIN msdb.dbo.syscategories c ON j.category_id = c.category_id
WHERE j.name = @JobName;

PRINT '';

-- Display schedule details
SELECT 
    s.name AS ScheduleName,
    s.enabled AS IsEnabled,
    CASE s.freq_type
        WHEN 1 THEN 'Once'
        WHEN 4 THEN 'Daily'
        WHEN 8 THEN 'Weekly'
        WHEN 16 THEN 'Monthly'
        WHEN 32 THEN 'Monthly relative'
        WHEN 64 THEN 'When SQL Server Agent starts'
        WHEN 128 THEN 'Start when idle'
    END AS FrequencyType,
    s.freq_interval AS FrequencyInterval,
    CASE s.freq_subday_type
        WHEN 1 THEN 'At specified time'
        WHEN 2 THEN 'Seconds'
        WHEN 4 THEN 'Minutes'
        WHEN 8 THEN 'Hours'
    END AS SubdayFrequencyType,
    s.freq_subday_interval AS SubdayInterval,
    s.active_start_date AS ActiveStartDate,
    s.active_start_time AS ActiveStartTime
FROM msdb.dbo.sysjobs j
INNER JOIN msdb.dbo.sysjobschedules js ON j.job_id = js.job_id
INNER JOIN msdb.dbo.sysschedules s ON js.schedule_id = s.schedule_id
WHERE j.name = @JobName;

PRINT '';
PRINT '========================================================';
PRINT 'Job provisioning completed successfully!';
PRINT '';
PRINT 'Job Name: ' + @JobName;
PRINT 'Schedule: Every 15 minutes';
PRINT 'Status: Enabled';
PRINT '';
PRINT 'To manually execute the job, run:';
PRINT '  EXEC msdb.dbo.sp_start_job @job_name = ''' + @JobName + ''';';
PRINT '';
PRINT 'To disable the job, run:';
PRINT '  EXEC msdb.dbo.sp_update_job @job_name = ''' + @JobName + ''', @enabled = 0;';
PRINT '';
PRINT 'To monitor job execution history:';
PRINT '  SELECT * FROM dbo.AutonomousImprovementHistory ORDER BY AnalysisTimestamp DESC;';
PRINT '========================================================';
GO

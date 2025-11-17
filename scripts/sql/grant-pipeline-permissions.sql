-- ============================================================================
-- Grant Azure DevOps Pipeline Agent Permissions for CLR Deployment
-- ============================================================================
-- This script is IDEMPOTENT and can be run multiple times safely.
-- It grants the necessary server-level permissions to the Azure DevOps agent
-- service account so it can configure CLR and deploy assemblies.
--
-- REQUIRED PERMISSIONS:
-- - ALTER SETTINGS: Required for sp_configure and RECONFIGURE
-- - CONTROL SERVER: Includes ALTER ANY DATABASE for TRUSTWORTHY
--
-- The script automatically detects the running agent service account.
-- ============================================================================

USE [master];
GO

SET NOCOUNT ON;
GO

-- Auto-detect Azure DevOps agent service account
DECLARE @AgentLogin NVARCHAR(256);
DECLARE @DetectionLog TABLE (LoginName NVARCHAR(256));

-- Try common agent service accounts
INSERT INTO @DetectionLog (LoginName)
SELECT name 
FROM sys.server_principals
WHERE name IN (
    N'NT AUTHORITY\NETWORK SERVICE',
    N'NT AUTHORITY\SYSTEM',
    N'NT SERVICE\vstsagent',
    N'NT SERVICE\AzureDevOpsAgent'
)
AND type_desc = 'WINDOWS_LOGIN';

-- If multiple found, prefer NETWORK SERVICE (most common for Local Agent Pool)
IF EXISTS (SELECT 1 FROM @DetectionLog WHERE LoginName = N'NT AUTHORITY\NETWORK SERVICE')
    SET @AgentLogin = N'NT AUTHORITY\NETWORK SERVICE';
ELSE IF EXISTS (SELECT 1 FROM @DetectionLog WHERE LoginName = N'NT AUTHORITY\SYSTEM')
    SET @AgentLogin = N'NT AUTHORITY\SYSTEM';
ELSE
    SELECT TOP 1 @AgentLogin = LoginName FROM @DetectionLog;

-- Fallback if no agent detected: default to NETWORK SERVICE and create it
IF @AgentLogin IS NULL
BEGIN
    SET @AgentLogin = N'NT AUTHORITY\NETWORK SERVICE';
    PRINT 'No existing agent login detected. Defaulting to: ' + @AgentLogin;
    
    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @AgentLogin)
    BEGIN
        PRINT 'Creating login: ' + @AgentLogin;
        DECLARE @CreateLoginSql NVARCHAR(MAX) = 'CREATE LOGIN ' + QUOTENAME(@AgentLogin) + ' FROM WINDOWS;';
        EXEC sp_executesql @CreateLoginSql;
    END
END
ELSE
BEGIN
    PRINT 'Detected agent login: ' + @AgentLogin;
END
GO

-- Grant ALTER SETTINGS permission (idempotent)
DECLARE @AgentLogin NVARCHAR(256);
SELECT TOP 1 @AgentLogin = name 
FROM sys.server_principals
WHERE name IN (
    N'NT AUTHORITY\NETWORK SERVICE',
    N'NT AUTHORITY\SYSTEM',
    N'NT SERVICE\vstsagent',
    N'NT SERVICE\AzureDevOpsAgent'
)
AND type_desc = 'WINDOWS_LOGIN'
ORDER BY 
    CASE name 
        WHEN N'NT AUTHORITY\NETWORK SERVICE' THEN 1
        WHEN N'NT AUTHORITY\SYSTEM' THEN 2
        ELSE 3
    END;

IF @AgentLogin IS NULL
    SET @AgentLogin = N'NT AUTHORITY\NETWORK SERVICE';

-- Check if permission already granted
IF NOT EXISTS (
    SELECT 1 
    FROM sys.server_permissions perm
    INNER JOIN sys.server_principals sp ON perm.grantee_principal_id = sp.principal_id
    WHERE sp.name = @AgentLogin 
    AND perm.permission_name = 'ALTER SETTINGS'
    AND perm.state_desc = 'GRANT'
)
BEGIN
    PRINT 'Granting ALTER SETTINGS to: ' + @AgentLogin;
    DECLARE @GrantAlterSettingsSql NVARCHAR(MAX) = 'GRANT ALTER SETTINGS TO ' + QUOTENAME(@AgentLogin) + ';';
    EXEC sp_executesql @GrantAlterSettingsSql;
END
ELSE
BEGIN
    PRINT 'ALTER SETTINGS already granted to: ' + @AgentLogin;
END
GO

-- Grant CONTROL SERVER permission (idempotent)
DECLARE @AgentLogin NVARCHAR(256);
SELECT TOP 1 @AgentLogin = name 
FROM sys.server_principals
WHERE name IN (
    N'NT AUTHORITY\NETWORK SERVICE',
    N'NT AUTHORITY\SYSTEM',
    N'NT SERVICE\vstsagent',
    N'NT SERVICE\AzureDevOpsAgent'
)
AND type_desc = 'WINDOWS_LOGIN'
ORDER BY 
    CASE name 
        WHEN N'NT AUTHORITY\NETWORK SERVICE' THEN 1
        WHEN N'NT AUTHORITY\SYSTEM' THEN 2
        ELSE 3
    END;

IF @AgentLogin IS NULL
    SET @AgentLogin = N'NT AUTHORITY\NETWORK SERVICE';

-- Check if permission already granted
IF NOT EXISTS (
    SELECT 1 
    FROM sys.server_permissions perm
    INNER JOIN sys.server_principals sp ON perm.grantee_principal_id = sp.principal_id
    WHERE sp.name = @AgentLogin 
    AND perm.permission_name = 'CONTROL SERVER'
    AND perm.state_desc = 'GRANT'
)
BEGIN
    PRINT 'Granting CONTROL SERVER to: ' + @AgentLogin;
    DECLARE @GrantControlServerSql NVARCHAR(MAX) = 'GRANT CONTROL SERVER TO ' + QUOTENAME(@AgentLogin) + ';';
    EXEC sp_executesql @GrantControlServerSql;
END
ELSE
BEGIN
    PRINT 'CONTROL SERVER already granted to: ' + @AgentLogin;
END
GO

-- Verify and report final state
DECLARE @AgentLogin NVARCHAR(256);
SELECT TOP 1 @AgentLogin = name 
FROM sys.server_principals
WHERE name IN (
    N'NT AUTHORITY\NETWORK SERVICE',
    N'NT AUTHORITY\SYSTEM',
    N'NT SERVICE\vstsagent',
    N'NT SERVICE\AzureDevOpsAgent'
)
AND type_desc = 'WINDOWS_LOGIN'
ORDER BY 
    CASE name 
        WHEN N'NT AUTHORITY\NETWORK SERVICE' THEN 1
        WHEN N'NT AUTHORITY\SYSTEM' THEN 2
        ELSE 3
    END;

IF @AgentLogin IS NULL
    SET @AgentLogin = N'NT AUTHORITY\NETWORK SERVICE';

PRINT '';
PRINT '========================================';
PRINT 'Azure DevOps Agent Permissions Summary';
PRINT '========================================';
PRINT 'Agent Login: ' + @AgentLogin;
PRINT '';
PRINT 'Granted Permissions:';

SELECT 
    '  - ' + perm.permission_name + ' (' + perm.state_desc + ')' AS Permission
FROM sys.server_permissions perm
INNER JOIN sys.server_principals sp ON perm.grantee_principal_id = sp.principal_id
WHERE sp.name = @AgentLogin
AND perm.permission_name IN ('ALTER SETTINGS', 'CONTROL SERVER', 'CONNECT SQL')
ORDER BY perm.permission_name;

PRINT '';
PRINT 'Server Roles:';

IF EXISTS (
    SELECT 1 
    FROM sys.server_role_members srm
    INNER JOIN sys.server_principals sp ON srm.member_principal_id = sp.principal_id
    WHERE sp.name = @AgentLogin
)
BEGIN
    SELECT 
        '  - ' + sr.name AS ServerRole
    FROM sys.server_principals sp
    INNER JOIN sys.server_role_members srm ON sp.principal_id = srm.member_principal_id
    INNER JOIN sys.server_principals sr ON srm.role_principal_id = sr.principal_id
    WHERE sp.name = @AgentLogin;
END
ELSE
BEGIN
    PRINT '  (None - using explicit permissions)';
END

PRINT '';
PRINT 'Status: Ready for pipeline deployment';
PRINT '========================================';
GO

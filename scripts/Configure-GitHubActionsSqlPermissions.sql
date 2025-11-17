-- ============================================================================
-- GitHub Actions Service Principal SQL Permissions (Idempotent)
-- Generated: 2025-11-17 04:24:19
-- ============================================================================

SET NOCOUNT ON;
GO

-- Development Environment
PRINT 'Configuring permissions for Hartonomous-GitHub-Actions-Development...';

-- Create login if not exists
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'Hartonomous-GitHub-Actions-Development')
BEGIN
    PRINT '  Creating login...';
    CREATE LOGIN [Hartonomous-GitHub-Actions-Development] FROM EXTERNAL PROVIDER;
END
ELSE
BEGIN
    PRINT '  Login already exists.';
END
GO

-- Create user in database if not exists
USE [Hartonomous];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'Hartonomous-GitHub-Actions-Development')
BEGIN
    PRINT '  Creating database user...';
    CREATE USER [Hartonomous-GitHub-Actions-Development] FROM LOGIN [Hartonomous-GitHub-Actions-Development];
END
ELSE
BEGIN
    PRINT '  Database user already exists.';
END
GO

-- Grant db_owner role (idempotent)
IF NOT IS_ROLEMEMBER('db_owner', 'Hartonomous-GitHub-Actions-Development') = 1
BEGIN
    PRINT '  Adding to db_owner role...';
    ALTER ROLE db_owner ADD MEMBER [Hartonomous-GitHub-Actions-Development];
END
ELSE
BEGIN
    PRINT '  Already member of db_owner role.';
END
GO

-- Grant server-level permissions
USE [master];
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.server_permissions sp
    JOIN sys.server_principals pr ON sp.grantee_principal_id = pr.principal_id
    WHERE pr.name = 'Hartonomous-GitHub-Actions-Development' AND sp.permission_name = 'VIEW SERVER STATE'
)
BEGIN
    PRINT '  Granting VIEW SERVER STATE...';
    GRANT VIEW SERVER STATE TO [Hartonomous-GitHub-Actions-Development];
END
ELSE
BEGIN
    PRINT '  VIEW SERVER STATE already granted.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.server_permissions sp
    JOIN sys.server_principals pr ON sp.grantee_principal_id = pr.principal_id
    WHERE pr.name = 'Hartonomous-GitHub-Actions-Development' AND sp.permission_name = 'ALTER ANY DATABASE'
)
BEGIN
    PRINT '  Granting ALTER ANY DATABASE...';
    GRANT ALTER ANY DATABASE TO [Hartonomous-GitHub-Actions-Development];
END
ELSE
BEGIN
    PRINT '  ALTER ANY DATABASE already granted.';
END
GO

PRINT '✓ Permissions configured for Hartonomous-GitHub-Actions-Development';
PRINT '';
GO

-- Staging Environment
PRINT 'Configuring permissions for Hartonomous-GitHub-Actions-Staging...';

-- Create login if not exists
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'Hartonomous-GitHub-Actions-Staging')
BEGIN
    PRINT '  Creating login...';
    CREATE LOGIN [Hartonomous-GitHub-Actions-Staging] FROM EXTERNAL PROVIDER;
END
ELSE
BEGIN
    PRINT '  Login already exists.';
END
GO

-- Create user in database if not exists
USE [Hartonomous];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'Hartonomous-GitHub-Actions-Staging')
BEGIN
    PRINT '  Creating database user...';
    CREATE USER [Hartonomous-GitHub-Actions-Staging] FROM LOGIN [Hartonomous-GitHub-Actions-Staging];
END
ELSE
BEGIN
    PRINT '  Database user already exists.';
END
GO

-- Grant db_owner role (idempotent)
IF NOT IS_ROLEMEMBER('db_owner', 'Hartonomous-GitHub-Actions-Staging') = 1
BEGIN
    PRINT '  Adding to db_owner role...';
    ALTER ROLE db_owner ADD MEMBER [Hartonomous-GitHub-Actions-Staging];
END
ELSE
BEGIN
    PRINT '  Already member of db_owner role.';
END
GO

-- Grant server-level permissions
USE [master];
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.server_permissions sp
    JOIN sys.server_principals pr ON sp.grantee_principal_id = pr.principal_id
    WHERE pr.name = 'Hartonomous-GitHub-Actions-Staging' AND sp.permission_name = 'VIEW SERVER STATE'
)
BEGIN
    PRINT '  Granting VIEW SERVER STATE...';
    GRANT VIEW SERVER STATE TO [Hartonomous-GitHub-Actions-Staging];
END
ELSE
BEGIN
    PRINT '  VIEW SERVER STATE already granted.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.server_permissions sp
    JOIN sys.server_principals pr ON sp.grantee_principal_id = pr.principal_id
    WHERE pr.name = 'Hartonomous-GitHub-Actions-Staging' AND sp.permission_name = 'ALTER ANY DATABASE'
)
BEGIN
    PRINT '  Granting ALTER ANY DATABASE...';
    GRANT ALTER ANY DATABASE TO [Hartonomous-GitHub-Actions-Staging];
END
ELSE
BEGIN
    PRINT '  ALTER ANY DATABASE already granted.';
END
GO

PRINT '✓ Permissions configured for Hartonomous-GitHub-Actions-Staging';
PRINT '';
GO

-- Production Environment
PRINT 'Configuring permissions for Hartonomous-GitHub-Actions-Production...';

-- Create login if not exists
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'Hartonomous-GitHub-Actions-Production')
BEGIN
    PRINT '  Creating login...';
    CREATE LOGIN [Hartonomous-GitHub-Actions-Production] FROM EXTERNAL PROVIDER;
END
ELSE
BEGIN
    PRINT '  Login already exists.';
END
GO

-- Create user in database if not exists
USE [Hartonomous];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'Hartonomous-GitHub-Actions-Production')
BEGIN
    PRINT '  Creating database user...';
    CREATE USER [Hartonomous-GitHub-Actions-Production] FROM LOGIN [Hartonomous-GitHub-Actions-Production];
END
ELSE
BEGIN
    PRINT '  Database user already exists.';
END
GO

-- Grant db_owner role (idempotent)
IF NOT IS_ROLEMEMBER('db_owner', 'Hartonomous-GitHub-Actions-Production') = 1
BEGIN
    PRINT '  Adding to db_owner role...';
    ALTER ROLE db_owner ADD MEMBER [Hartonomous-GitHub-Actions-Production];
END
ELSE
BEGIN
    PRINT '  Already member of db_owner role.';
END
GO

-- Grant server-level permissions
USE [master];
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.server_permissions sp
    JOIN sys.server_principals pr ON sp.grantee_principal_id = pr.principal_id
    WHERE pr.name = 'Hartonomous-GitHub-Actions-Production' AND sp.permission_name = 'VIEW SERVER STATE'
)
BEGIN
    PRINT '  Granting VIEW SERVER STATE...';
    GRANT VIEW SERVER STATE TO [Hartonomous-GitHub-Actions-Production];
END
ELSE
BEGIN
    PRINT '  VIEW SERVER STATE already granted.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.server_permissions sp
    JOIN sys.server_principals pr ON sp.grantee_principal_id = pr.principal_id
    WHERE pr.name = 'Hartonomous-GitHub-Actions-Production' AND sp.permission_name = 'ALTER ANY DATABASE'
)
BEGIN
    PRINT '  Granting ALTER ANY DATABASE...';
    GRANT ALTER ANY DATABASE TO [Hartonomous-GitHub-Actions-Production];
END
ELSE
BEGIN
    PRINT '  ALTER ANY DATABASE already granted.';
END
GO

PRINT '✓ Permissions configured for Hartonomous-GitHub-Actions-Production';
PRINT '';
GO

PRINT '═══════════════════════════════════════════════════════════════';
PRINT 'SQL permissions configuration complete.';
PRINT '═══════════════════════════════════════════════════════════════';
GO

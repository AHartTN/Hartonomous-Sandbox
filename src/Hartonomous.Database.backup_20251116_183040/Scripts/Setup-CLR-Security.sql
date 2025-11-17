/*
    CLR Security Setup Script
    
    This script must be executed BEFORE deploying the DACPAC.
    It sets up the asymmetric key-based security required for UNSAFE CLR assemblies.
    
    Why separate from DACPAC:
    - CREATE ASYMMETRIC KEY FROM FILE is incompatible with DACFx
    - This is a one-time server/database setup, not part of schema deployment
    
    Execute with:
    sqlcmd -S localhost -d Hartonomous -E -i Setup-CLR-Security.sql
*/

USE [master];
GO

PRINT '=== CLR Security Setup ==='
PRINT 'Setting up asymmetric key-based authentication for UNSAFE assemblies';
PRINT '';

-- Step 0: Create master key in master database if it doesn't exist
PRINT 'Step 0: Creating master database master key...';
IF NOT EXISTS (SELECT 1 FROM sys.symmetric_keys WHERE name = '##MS_DatabaseMasterKey##')
BEGIN
    CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'CLR_Security_MasterKey_2024!@#';
    PRINT '  ✓ Master key created';
END
ELSE
    PRINT '  ○ Master key already exists';

GO

-- Step 1: Create asymmetric key from strong name key file
-- Path should be adjusted based on your repository location
-- Default assumes this script is run from src\Hartonomous.Database\Scripts
PRINT 'Step 1: Creating asymmetric key from SqlClrKey.snk...';
IF NOT EXISTS (SELECT 1 FROM sys.asymmetric_keys WHERE name = 'SqlClrAsymmetricKey')
BEGIN
    -- Direct SQL - CREATE ASYMMETRIC KEY cannot be executed via sp_executesql
    -- Note: No WITH ALGORITHM clause when loading from file (algorithm is determined from file)
    CREATE ASYMMETRIC KEY [SqlClrAsymmetricKey] 
    FROM FILE = 'D:\Repositories\Hartonomous\src\Hartonomous.Database\CLR\SqlClrKey.snk';
    
    PRINT '  ✓ Asymmetric key created from: D:\Repositories\Hartonomous\src\Hartonomous.Database\CLR\SqlClrKey.snk';
END
ELSE
    PRINT '  ○ Asymmetric key already exists';

GO

-- Step 2: Create login from asymmetric key
PRINT 'Step 2: Creating login from asymmetric key...';
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'SqlClrLogin')
BEGIN
    CREATE LOGIN [SqlClrLogin]
    FROM ASYMMETRIC KEY [SqlClrAsymmetricKey];
    
    PRINT '  ✓ Login created';
END
ELSE
    PRINT '  ○ Login already exists';

GO

-- Step 3: Grant UNSAFE ASSEMBLY permission to login
PRINT 'Step 3: Granting UNSAFE ASSEMBLY permission...';
IF NOT EXISTS (
    SELECT 1 
    FROM sys.server_permissions sp
    JOIN sys.server_principals spr ON sp.grantee_principal_id = spr.principal_id
    WHERE spr.name = 'SqlClrLogin'
    AND sp.permission_name = 'UNSAFE ASSEMBLY'
    AND sp.state = 'G'
)
BEGIN
    GRANT UNSAFE ASSEMBLY TO [SqlClrLogin];
    PRINT '  ✓ UNSAFE ASSEMBLY permission granted';
END
ELSE
    PRINT '  ○ UNSAFE ASSEMBLY permission already granted';

GO

-- Step 4: Enable CLR integration at server level
PRINT 'Step 4: Enabling CLR integration...';
DECLARE @clr_enabled INT;
SELECT @clr_enabled = CAST(value_in_use AS INT) 
FROM sys.configurations 
WHERE name = 'clr enabled';

IF @clr_enabled = 0
BEGIN
    EXEC sp_configure 'clr enabled', 1;
    RECONFIGURE;
    PRINT '  ✓ CLR integration enabled';
END
ELSE
    PRINT '  ○ CLR integration already enabled';

GO

-- Step 5: Enable CLR strict security (SQL Server 2017+)
PRINT 'Step 5: Enabling CLR strict security...';
DECLARE @clr_strict_security INT;
SELECT @clr_strict_security = CAST(value_in_use AS INT) 
FROM sys.configurations 
WHERE name = 'clr strict security';

IF @clr_strict_security = 0
BEGIN
    EXEC sp_configure 'clr strict security', 1;
    RECONFIGURE;
    PRINT '  ✓ CLR strict security enabled';
END
ELSE
    PRINT '  ○ CLR strict security already enabled';

GO

PRINT '';
PRINT '=== CLR Security Setup Complete ===';
PRINT 'You can now deploy the DACPAC with CLR assemblies.';
GO

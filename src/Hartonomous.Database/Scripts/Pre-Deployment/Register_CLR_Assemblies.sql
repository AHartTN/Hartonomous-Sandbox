/*
================================================================================
CLR Assembly Registration - SqlClrFunctions (PRE-DEPLOYMENT)
================================================================================
Purpose: Registers the SqlClrFunctions.dll assembly BEFORE DACPAC deployment
         to enable CLR functions, aggregates, and procedures.

WHY PRE-DEPLOYMENT:
- DACPAC functions reference [SqlClrFunctions] assembly in their definitions
- Assembly MUST exist BEFORE functions are created
- CREATE ASSEMBLY must happen first, CREATE FUNCTION second (MS Docs pattern)

Requirements:
- SQL Server CLR integration enabled: sp_configure 'clr enabled', 1
- Database trustworthy OR assembly signed with asymmetric key
- UNSAFE permission required for:
  - File I/O operations
  - Audio signal processing (RMS, Peak, FFT)
  - Vector operations (normalization, cosine similarity)
  - Statistical computations (Z-score, churn prediction)

SQLCMD Variables:
- $(SqlClrDllPath): Full path to SqlClrFunctions.dll
  Example: D:\Repositories\Hartonomous\src\SqlClr\bin\Release\SqlClrFunctions.dll

Security Note:
- UNSAFE assemblies require sysadmin permissions or certificate/asymmetric key signing
- Review SqlClrFunctions.dll code before deployment to production
================================================================================
*/

SET NOCOUNT ON;
PRINT '=======================================================';
PRINT 'CLR ASSEMBLY REGISTRATION - SqlClrFunctions';
PRINT '=======================================================';
PRINT '';

-- Check if CLR integration is enabled
DECLARE @clr_enabled INT;
SELECT @clr_enabled = CAST(value AS INT) 
FROM sys.configurations 
WHERE name = 'clr enabled';

IF @clr_enabled = 0
BEGIN
    PRINT '⚠ CLR integration is DISABLED. Enabling CLR...';
    EXEC sp_configure 'clr enabled', 1;
    RECONFIGURE;
    PRINT '✓ CLR integration enabled';
END
ELSE
    PRINT '○ CLR integration already enabled';

GO

-- Drop existing assembly if it exists
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'SqlClrFunctions')
BEGIN
    PRINT '  Dropping existing SqlClrFunctions assembly...';
    
    -- Must drop dependent objects first (functions, aggregates, procedures)
    -- DACPAC has CREATE statements for these, so they'll be recreated
    DROP ASSEMBLY [SqlClrFunctions];
    
    PRINT '  ✓ Existing assembly dropped';
END
GO

-- Register the assembly with UNSAFE permissions
PRINT '  Registering SqlClrFunctions.dll with UNSAFE permissions...';

-- SQLCMD variable should point to FULL path: D:\...\SqlClr\bin\Release\SqlClrFunctions.dll
CREATE ASSEMBLY [SqlClrFunctions]
FROM '$(SqlClrDllPath)'
WITH PERMISSION_SET = UNSAFE;

PRINT '✓ SqlClrFunctions assembly registered successfully';
PRINT '  Assembly will be available to all CLR functions defined in DACPAC';
PRINT '';
GO

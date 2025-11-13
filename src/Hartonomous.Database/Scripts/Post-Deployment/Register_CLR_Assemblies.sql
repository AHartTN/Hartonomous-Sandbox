/*
================================================================================
CLR Assembly Registration - SqlClrFunctions
================================================================================
Purpose: Registers the SqlClrFunctions.dll assembly with UNSAFE permissions
         to enable CLR functions, aggregates, and procedures.

Requirements:
- Runs AFTER DACPAC deployment (functions/procedures must exist)
- SQL Server CLR integration enabled: sp_configure 'clr enabled', 1
- Database trustworthy OR assembly signed with asymmetric key
- UNSAFE permission required for:
  - File I/O operations
  - Audio signal processing (RMS, Peak, FFT)
  - Vector operations (normalization, cosine similarity)
  - Statistical computations (Z-score, churn prediction)

SQLCMD Variables:
- $(DacpacBinPath): Path to compiled assemblies (e.g., bin\Debug\)

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

-- Note: $(DacpacBinPath) should point to the directory containing SqlClrFunctions.dll
-- Example: /var:DacpacBinPath="D:\Hartonomous\bin\Debug\"
CREATE ASSEMBLY [SqlClrFunctions]
FROM '$(DacpacBinPath)SqlClrFunctions.dll'
WITH PERMISSION_SET = UNSAFE;

PRINT '✓ SqlClrFunctions assembly registered successfully';
PRINT '';

-- Display registered CLR functions/aggregates
PRINT 'Registered CLR Functions:';
SELECT 
    SCHEMA_NAME(o.schema_id) + '.' + o.name AS FunctionName,
    o.type_desc AS ObjectType
FROM sys.objects o
INNER JOIN sys.assembly_modules am ON o.object_id = am.object_id
INNER JOIN sys.assemblies a ON am.assembly_id = a.assembly_id
WHERE a.name = 'SqlClrFunctions'
ORDER BY o.type_desc, o.name;

PRINT '';
PRINT '✓ CLR assembly registration complete';
PRINT '';

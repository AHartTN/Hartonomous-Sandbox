-- Update Hartonomous.Clr assembly with new FileSystemFunctions.cs

USE Hartonomous;
GO

-- Step 1: Remove old trusted assembly hash
DECLARE @oldHash VARBINARY(64);
SELECT @oldHash = hash
FROM sys.trusted_assemblies
WHERE description = N'Hartonomous.Clr for Hartonomous autonomous improvement';

IF @oldHash IS NOT NULL
BEGIN
    EXEC sys.sp_drop_trusted_assembly @hash = @oldHash;
    PRINT 'Removed old trusted assembly hash';
END
GO

-- Step 2: Add new trusted assembly hash for updated DLL
DECLARE @newHash VARBINARY(64);

-- Calculate hash from file
DECLARE @assemblyBinary VARBINARY(MAX);
SELECT @assemblyBinary = BulkColumn
FROM OPENROWSET(BULK N'd:\Repositories\Hartonomous\src\SqlClr\bin\Release\Hartonomous.Clr.dll', SINGLE_BLOB) AS assembly_file;

SET @newHash = HASHBYTES('SHA2_512', @assemblyBinary);

EXEC sys.sp_add_trusted_assembly 
    @hash = @newHash, 
    @description = N'Hartonomous.Clr for Hartonomous autonomous improvement';

PRINT 'Added new trusted assembly hash';
PRINT 'Hash: ' + CONVERT(NVARCHAR(200), @newHash, 1);
GO

-- Step 3: Update the assembly
ALTER ASSEMBLY Hartonomous.Clr
FROM 'd:\Repositories\Hartonomous\src\SqlClr\bin\Release\Hartonomous.Clr.dll';

PRINT 'Assembly updated with FileSystemFunctions.cs';
GO

-- Step 4: Verify
SELECT name, permission_set_desc, create_date, modify_date
FROM sys.assemblies
WHERE name = 'Hartonomous.Clr';
GO

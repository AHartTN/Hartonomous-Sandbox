/*
================================================================================
Pre-Deployment Script - Environment-Specific Setup
================================================================================
Executed BEFORE deployment plan (but plan calculated before script runs)

Requirements:
- CLR Assembly Registration: MUST be first (functions reference assembly)
  Assembly: SqlClrFunctions.dll with UNSAFE permissions

- FILESTREAM filegroup: Required for binary payload storage
  Tables: TensorAtomPayloads, LayerTensorSegments, AtomPayloadStore
  
- In-Memory OLTP filegroup: Required for memory-optimized tables
  Tables: BillingUsageLedger_InMemory

SQLCMD Variables:
- $(SqlClrDllPath): Full path to SqlClrFunctions.dll (e.g., D:\...\SqlClr\bin\Release\SqlClrFunctions.dll)
- $(DefaultDataPath): Physical directory for database files (environment-specific)
- $(DatabaseName): Target database name
================================================================================
*/

PRINT 'Starting pre-deployment setup...';
GO

-- CLR assembly registration (dependencies only - main assembly deployed by DACPAC)
:r .\Register_CLR_Assemblies.sql

-- FILESTREAM filegroup (required for VARBINARY(MAX) FILESTREAM columns)
:r .\Setup_FILESTREAM_Filegroup.sql

-- In-Memory OLTP filegroup (required for MEMORY_OPTIMIZED = ON tables)
:r .\Setup_InMemory_Filegroup.sql

PRINT 'Pre-deployment setup complete.';
GO

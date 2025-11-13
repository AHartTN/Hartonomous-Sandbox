/*
================================================================================
Pre-Deployment Script - Environment-Specific Setup
================================================================================
Executed BEFORE deployment plan (but plan calculated before script runs)

Requirements:
- FILESTREAM filegroup: Required for binary payload storage
  Tables: TensorAtomPayloads, LayerTensorSegments, AtomPayloadStore
  
- In-Memory OLTP filegroup: Required for memory-optimized tables
  Tables: BillingUsageLedger_InMemory

SQLCMD Variables:
- $(DefaultDataPath): Physical directory for database files (environment-specific)
- $(DatabaseName): Target database name
================================================================================
*/

PRINT 'Starting pre-deployment setup...';
GO

-- FILESTREAM filegroup (required for VARBINARY(MAX) FILESTREAM columns)
:r .\Setup_FILESTREAM_Filegroup.sql

-- In-Memory OLTP filegroup (required for MEMORY_OPTIMIZED = ON tables)
:r .\Setup_InMemory_Filegroup.sql

PRINT 'Pre-deployment setup complete.';
GO

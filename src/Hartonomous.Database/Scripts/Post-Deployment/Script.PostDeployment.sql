/*
================================================================================
Post-Deployment Script - Configuration & Runtime Setup
================================================================================
Executed AFTER DACPAC deployment completes

Order of Execution:
1. CLR Assembly Registration - Must run before CLR functions can execute
2. Database Configuration - QueryStore, AutomaticTuning, CDC
3. Schema Modifications - Graph indexes, temporal tables, columnstore
4. Data Seeding - Initial data, agent tools, Service Broker activation
5. Table Compression - ROW/PAGE compression optimization

SQLCMD Variables:
- $(DacpacBinPath): Path to compiled assemblies for CLR registration
- $(DatabaseName): Target database name
================================================================================
*/

PRINT 'Starting post-deployment script execution...';
GO

-- CRITICAL: CLR assembly registration MUST run first
-- All CLR functions/aggregates depend on this assembly being registered
-- Requires UNSAFE permissions for file I/O, audio processing, vector operations
:r .\Register_CLR_Assemblies.sql

-- Database-level configuration (can only run after database exists)
:r ..\Pre-Deployment\Enable_QueryStore.sql
:r ..\Pre-Deployment\Enable_AutomaticTuning.sql

-- CDC requires tables to exist first
:r ..\Pre-Deployment\Enable_CDC.sql

-- Post-deployment schema modifications
:r .\graph.AtomGraphEdges_Add_PseudoColumn_Indexes.sql
:r .\Temporal_Tables_Add_Retention_and_Columnstore.sql
:r .\TensorAtomCoefficients_Temporal.sql
:r .\zz_consolidated_indexes.sql

-- Table compression optimization (ROW/PAGE compression)
:r .\Optimize_ColumnstoreCompression.sql

-- Data seeding and configuration
:r .\Seed.TopicKeywords.sql
:r .\RegisterAgentTools.sql
:r .\Configure.Neo4jSyncActivation.sql
:r .\Configure.InferenceQueueActivation.sql

PRINT 'Post-deployment script execution complete.';
GO
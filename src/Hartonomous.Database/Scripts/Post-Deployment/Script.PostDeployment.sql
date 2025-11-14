/*
================================================================================
Post-Deployment Script - Configuration & Runtime Setup
================================================================================
Executed AFTER DACPAC deployment completes

Order of Execution:
1. Database Configuration - QueryStore, AutomaticTuning, CDC
2. Schema Modifications - Graph indexes, temporal tables, columnstore
3. Data Seeding - Initial data, agent tools, Service Broker activation
4. Table Compression - ROW/PAGE compression optimization

SQLCMD Variables:
- $(DatabaseName): Target database name

NOTE: CLR Assembly Registration moved to PRE-DEPLOYMENT (must happen before functions are created)
================================================================================
*/

PRINT 'Starting post-deployment script execution...';
GO

-- Database-level configuration (can only run after database exists)
:r ..\Pre-Deployment\Enable_QueryStore.sql
:r ..\Pre-Deployment\Enable_AutomaticTuning.sql

-- CDC moved to PRE-DEPLOYMENT ONLY (handled via deploy-dacpac.ps1 in separate transaction)
-- Reason: CDC schema doesn't exist until first enable, causes column reference errors in post-deployment

-- In-Memory OLTP (Hekaton) Setup - MUST run before natively-compiled procedures
:r .\Setup_InMemory_Tables.sql
:r .\Create_NativelyCompiled_Procedures.sql

-- Post-deployment schema modifications
:r .\graph.AtomGraphEdges_Add_PseudoColumn_Indexes.sql
:r .\Temporal_Tables_Add_Retention_and_Columnstore.sql
:r .\zz_consolidated_indexes.sql

-- Table compression optimization (ROW/PAGE compression)
:r .\Optimize_ColumnstoreCompression.sql

-- Data seeding and configuration
:r .\Seed.RefStatus.sql
:r .\Seed.TopicKeywords.sql
:r .\RegisterAgentTools.sql
:r .\Configure.Neo4jSyncActivation.sql
:r .\Configure.InferenceQueueActivation.sql

PRINT 'Post-deployment script execution complete.';
GO
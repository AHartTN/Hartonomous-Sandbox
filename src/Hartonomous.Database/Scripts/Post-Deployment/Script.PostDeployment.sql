/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/
PRINT 'Starting post-deployment script execution...';
GO

:r .\graph.AtomGraphEdges_Add_PseudoColumn_Indexes.sql
:r .\Temporal_Tables_Add_Retention_and_Columnstore.sql
:r .\TensorAtomCoefficients_Temporal.sql
:r .\zz_consolidated_indexes.sql

PRINT 'Post-deployment script execution complete.';
GO

:r .\Seed.TopicKeywords.sql

:r .\Configure.Neo4jSyncActivation.sql

:r .\Configure.InferenceQueueActivation.sql

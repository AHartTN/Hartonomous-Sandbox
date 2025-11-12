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
:r .\Seed.TopicKeywords.sql
:r .\RegisterAgentTools.sql
:r .\Configure.Neo4jSyncActivation.sql
:r .\Configure.InferenceQueueActivation.sql

PRINT 'Including Service Broker objects...';
GO

:r ..\..\ServiceBroker\Services\dbo.LearnService.sql
:r ..\..\ServiceBroker\Services\dbo.ActService.sql
:r ..\..\ServiceBroker\Services\dbo.HypothesizeService.sql
:r ..\..\ServiceBroker\Services\dbo.AnalyzeService.sql
:r ..\..\ServiceBroker\Services\dbo.Initiator.sql
:r ..\..\ServiceBroker\Queues\dbo.InitiatorQueue.sql
:r ..\..\ServiceBroker\Queues\dbo.LearnQueue.sql
:r ..\..\ServiceBroker\Queues\dbo.ActQueue.sql
:r ..\..\ServiceBroker\Queues\dbo.HypothesizeQueue.sql
:r ..\..\ServiceBroker\Queues\dbo.AnalyzeQueue.sql
:r ..\..\ServiceBroker\Contracts\dbo.LearnContract.sql
:r ..\..\ServiceBroker\Contracts\dbo.ActContract.sql
:r ..\..\ServiceBroker\Contracts\dbo.HypothesizeContract.sql
:r ..\..\ServiceBroker\Contracts\dbo.AnalyzeContract.sql
:r ..\..\ServiceBroker\MessageTypes\dbo.LearnMessage.sql
:r ..\..\ServiceBroker\MessageTypes\dbo.ActMessage.sql
:r ..\..\ServiceBroker\MessageTypes\dbo.HypothesizeMessage.sql
:r ..\..\ServiceBroker\MessageTypes\dbo.AnalyzeMessage.sql
:r ..\..\ServiceBroker\Services\Neo4jSyncService.sql
:r ..\..\ServiceBroker\Queues\dbo.Neo4jSyncQueue.sql
:r ..\..\ServiceBroker\Contracts\Neo4jSyncContract.sql
:r ..\..\ServiceBroker\MessageTypes\Neo4jSyncRequest.sql
:r ..\..\ServiceBroker\Services\InferenceService.sql
:r ..\..\ServiceBroker\Queues\dbo.InferenceQueue.sql
:r ..\..\ServiceBroker\Contracts\InferenceJobContract.sql
:r ..\..\ServiceBroker\MessageTypes\InferenceJobResponse.sql
:r ..\..\ServiceBroker\MessageTypes\InferenceJobRequest.sql
:r ..\..\ServiceBroker\Services\InitiatorService.sql
:r ..\..\ServiceBroker\InferenceJobRequest.MessageType.sql
:r ..\..\ServiceBroker\InferenceJobResponse.MessageType.sql
:r ..\..\ServiceBroker\Neo4jSyncRequest.sql

PRINT 'Post-deployment script execution complete.';
GO
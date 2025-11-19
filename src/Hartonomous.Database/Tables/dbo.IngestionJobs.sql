CREATE TABLE [dbo].[IngestionJobs]
(
    [IngestionJobId] BIGINT NOT NULL PRIMARY KEY IDENTITY, 
    [JobStatus] NVARCHAR(50) NOT NULL, 
    [AtomChunkSize] INT NOT NULL, 
    [CurrentAtomOffset] BIGINT NOT NULL, 
    [AtomQuota] BIGINT NOT NULL, 
    [TotalAtomsProcessed] BIGINT NOT NULL, 
    [ParentAtomId] BIGINT NULL, 
    [TenantId] INT NULL, 
    [ModelId] INT NULL, 
    [LastUpdatedAt] DATETIME2(7) NOT NULL, 
    [ErrorMessage] NVARCHAR(MAX) NULL
)

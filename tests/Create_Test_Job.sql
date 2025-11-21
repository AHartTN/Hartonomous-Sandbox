USE Hartonomous;
GO

SET QUOTED_IDENTIFIER ON;
GO

INSERT INTO dbo.BackgroundJob (
    JobType, 
    Payload, 
    Status, 
    TenantId, 
    Priority, 
    MaxRetries, 
    AttemptCount, 
    CreatedAtUtc
) 
VALUES (
    'GenerateEmbedding',
    '{"AtomId": 10, "TenantId": 0, "Modality": "text"}',
    0, -- Pending
    0, -- TenantId
    5, -- Priority
    3, -- MaxRetries
    0, -- AttemptCount
    GETUTCDATE()
);

SELECT SCOPE_IDENTITY() AS JobId, 'Job created successfully' AS Status;
GO

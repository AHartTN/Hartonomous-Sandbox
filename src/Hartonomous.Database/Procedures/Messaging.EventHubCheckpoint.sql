SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID('dbo.EventHubCheckpoints', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EventHubCheckpoints
    (
        CheckpointId            UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_EventHubCheckpoints_CheckpointId DEFAULT NEWSEQUENTIALID(),
        FullyQualifiedNamespace NVARCHAR(256) NOT NULL,
        EventHubName            NVARCHAR(256) NOT NULL,
        ConsumerGroup           NVARCHAR(256) NOT NULL,
        PartitionId             NVARCHAR(64)  NOT NULL,
        OwnerIdentifier         NVARCHAR(256) NULL,
        SequenceNumber          BIGINT        NULL,
        Offset                  BIGINT        NULL,
        LastModifiedTimeUtc     DATETIME2(7)  NOT NULL CONSTRAINT DF_EventHubCheckpoints_LastModified DEFAULT SYSUTCDATETIME(),
        ETag                    NVARCHAR(36)  NOT NULL CONSTRAINT DF_EventHubCheckpoints_Etag DEFAULT CONVERT(NVARCHAR(36), NEWID()),
        UniqueKeyHash           AS CONVERT(VARBINARY(32), HASHBYTES('SHA2_256', CONCAT(FullyQualifiedNamespace, N'|', EventHubName, N'|', ConsumerGroup, N'|', PartitionId))) PERSISTED,
        CONSTRAINT PK_EventHubCheckpoints PRIMARY KEY CLUSTERED (CheckpointId)
    );

    CREATE UNIQUE INDEX UX_EventHubCheckpoints_Composite
        ON dbo.EventHubCheckpoints (UniqueKeyHash);

    CREATE INDEX IX_EventHubCheckpoints_Owner
        ON dbo.EventHubCheckpoints (OwnerIdentifier)
        WHERE OwnerIdentifier IS NOT NULL;
END;

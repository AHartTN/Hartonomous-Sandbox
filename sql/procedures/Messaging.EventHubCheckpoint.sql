IF OBJECT_ID('dbo.EventHubCheckpoints', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EventHubCheckpoints
    (
        FullyQualifiedNamespace NVARCHAR(256) NOT NULL,
        EventHubName            NVARCHAR(256) NOT NULL,
        ConsumerGroup           NVARCHAR(256) NOT NULL,
        PartitionId             NVARCHAR(64)  NOT NULL,
        OwnerIdentifier         NVARCHAR(256) NULL,
        SequenceNumber          BIGINT        NULL,
        Offset                  BIGINT        NULL,
        LastModifiedTimeUtc     DATETIME2(7)  NOT NULL CONSTRAINT DF_EventHubCheckpoints_LastModified DEFAULT SYSUTCDATETIME(),
        ETag                    NVARCHAR(36)  NOT NULL CONSTRAINT DF_EventHubCheckpoints_Etag DEFAULT CONVERT(NVARCHAR(36), NEWID()),
        CONSTRAINT PK_EventHubCheckpoints PRIMARY KEY (FullyQualifiedNamespace, EventHubName, ConsumerGroup, PartitionId)
    );

    CREATE INDEX IX_EventHubCheckpoints_Owner
        ON dbo.EventHubCheckpoints (OwnerIdentifier)
        WHERE OwnerIdentifier IS NOT NULL;
END;
GO

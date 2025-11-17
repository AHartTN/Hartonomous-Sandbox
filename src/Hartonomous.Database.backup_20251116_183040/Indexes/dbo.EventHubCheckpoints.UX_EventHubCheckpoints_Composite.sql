CREATE UNIQUE INDEX UX_EventHubCheckpoints_Composite
    ON dbo.EventHubCheckpoints (UniqueKeyHash);
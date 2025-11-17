CREATE INDEX IX_EventHubCheckpoints_Owner
    ON dbo.EventHubCheckpoints (OwnerIdentifier)
    WHERE OwnerIdentifier IS NOT NULL;
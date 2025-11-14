-- ==================================================================
-- IX_Status_Active_Code: Covering index for active status lookups
-- ==================================================================
CREATE NONCLUSTERED INDEX [IX_Status_Active_Code]
    ON [ref].[Status]([IsActive], [Code])
    INCLUDE ([StatusId], [Name])
    WHERE [IsActive] = 1;

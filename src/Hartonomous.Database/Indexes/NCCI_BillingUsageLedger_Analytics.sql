-- Nonclustered columnstore index for analytical queries on BillingUsageLedger
-- Enables fast aggregation queries on tenant usage patterns while keeping rowstore for OLTP
CREATE NONCLUSTERED COLUMNSTORE INDEX [NCCI_BillingUsageLedger_Analytics]
    ON [dbo].[BillingUsageLedger]
    (
        [TenantId],
        [PrincipalId],
        [Operation],
        [MessageType],
        [Handler],
        [Units],
        [BaseRate],
        [Multiplier],
        [TotalCost],
        [TimestampUtc]
    );

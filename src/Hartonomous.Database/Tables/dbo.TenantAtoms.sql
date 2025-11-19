CREATE TABLE [dbo].[TenantAtoms]
(
    [TenantAtomId] BIGINT NOT NULL PRIMARY KEY IDENTITY, 
    [TenantId] INT NOT NULL, 
    [AtomId] BIGINT NOT NULL
)

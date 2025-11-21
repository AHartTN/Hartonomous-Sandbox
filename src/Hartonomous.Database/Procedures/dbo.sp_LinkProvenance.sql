CREATE OR ALTER PROCEDURE dbo.sp_LinkProvenance
    @ParentAtomIds NVARCHAR(MAX), -- Comma-separated list
    @ChildAtomId BIGINT,
    @DependencyType NVARCHAR(50) = 'DerivedFrom',
    @TenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        DECLARE @ParentAtoms TABLE (AtomId BIGINT);
        
        -- Parse parent atom IDs
        INSERT INTO @ParentAtoms
        SELECT CAST(value AS BIGINT)
        FROM STRING_SPLIT(@ParentAtomIds, ',');
        
        -- Create graph edges (parent → child)
        INSERT INTO provenance.AtomGraphEdges (FromAtomId, ToAtomId, DependencyType, TenantId)
        SELECT 
            pa.AtomId,
            @ChildAtomId,
            @DependencyType,
            @TenantId
        FROM @ParentAtoms pa
        WHERE NOT EXISTS (
            -- Avoid duplicate edges
            SELECT 1 
            FROM provenance.AtomGraphEdges edge
            WHERE edge.FromAtomId = pa.AtomId 
                  AND edge.ToAtomId = @ChildAtomId
        );
        
        DECLARE @EdgesCreated INT = @@ROWCOUNT;
        
        COMMIT TRANSACTION;
        
        SELECT 
            @ChildAtomId AS ChildAtomId,
            @EdgesCreated AS EdgesCreated,
            @DependencyType AS DependencyType;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
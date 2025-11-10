-- sp_AtomizeCode: AST-as-GEOMETRY pipeline for source code ingestion
-- Parses source code using Roslyn, generates structural vector, projects to GEOMETRY, stores in CodeAtom table

IF OBJECT_ID('dbo.sp_AtomizeCode', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_AtomizeCode;

CREATE OR ALTER PROCEDURE dbo.sp_AtomizeCode
    @AtomId BIGINT,
    @TenantId INT = 0,
    @Language NVARCHAR(50) = 'csharp',  -- Future: support multiple languages
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        -- ==========================================================================================
        -- Phase 1: Retrieve source code from Atom
        -- ==========================================================================================


        SELECT 
            @SourceCode = CAST(Content AS NVARCHAR(MAX)),
            @ContentType = ContentType
        FROM dbo.Atoms
        WHERE AtomId = @AtomId AND TenantId = @TenantId;

        IF @SourceCode IS NULL
        BEGIN
            RAISERROR('AtomId %I64d not found or has no content.', 16, 1, @AtomId);
            RETURN;
        END

        -- ==========================================================================================
        -- Phase 2: Generate AST structural vector using Roslyn CLR function
        -- ==========================================================================================

        SET @AstVectorJson = dbo.clr_GenerateCodeAstVector(@SourceCode);

        IF @AstVectorJson IS NULL OR JSON_VALUE(@AstVectorJson, '$.error') IS NOT NULL
        BEGIN

            RAISERROR('Failed to generate AST vector: %s', 16, 1, @parseError);
            RETURN;
        END

        -- ==========================================================================================
        -- Phase 3: Project 512-dimensional AST vector to 3D GEOMETRY using landmark projection
        -- ==========================================================================================

        SET @ProjectedPoint = dbo.clr_ProjectToPoint(@AstVectorJson);

        IF @ProjectedPoint IS NULL OR JSON_VALUE(@ProjectedPoint, '$.error') IS NOT NULL
        BEGIN

            RAISERROR('Failed to project AST vector to 3D: %s', 16, 1, @projectionError);
            RETURN;
        END



        -- ==========================================================================================
        -- Phase 4: Store AST representation in CodeAtom table
        -- ==========================================================================================

        SET @EmbeddingGeometry = geometry::STPointFromText(
            'POINT(' + CAST(@X AS NVARCHAR(50)) + ' ' + CAST(@Y AS NVARCHAR(50)) + ' ' + CAST(@Z AS NVARCHAR(50)) + ')',
            4326
        );

        -- Check if CodeAtom already exists for this AtomId
        IF EXISTS (SELECT 1 FROM dbo.CodeAtom WHERE AtomId = @AtomId)
        BEGIN
            -- Update existing CodeAtom
            UPDATE dbo.CodeAtom
            SET 
                Embedding = @EmbeddingGeometry,
                AstVector = @AstVectorJson,
                Language = @Language,
                UpdatedAt = GETUTCDATE()
            WHERE AtomId = @AtomId;

            IF @Debug = 1
                PRINT 'Updated CodeAtom for AtomId ' + CAST(@AtomId AS NVARCHAR(20));
        END
        ELSE
        BEGIN
            -- Insert new CodeAtom
            

            IF @Debug = 1
                PRINT 'Created CodeAtom for AtomId ' + CAST(@AtomId AS NVARCHAR(20));
        END

        -- ==========================================================================================
        -- Phase 5: Update parent Atom with spatial key
        -- ==========================================================================================
        UPDATE dbo.Atoms
        SET SpatialKey = @EmbeddingGeometry
        WHERE AtomId = @AtomId;

    END TRY
    BEGIN CATCH
        -- Rethrow the error to be caught by the caller
        THROW;
    END CATCH;
END;

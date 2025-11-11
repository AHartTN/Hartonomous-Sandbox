-- sp_ExtractStudentModel: Student Model Factory - Shape-to-Model Synthesis
-- Queries tensor atoms by spatial shape, retrieves SVD components, synthesizes new model layer

USE Hartonomous;
GO

IF OBJECT_ID('dbo.sp_ExtractStudentModel', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_ExtractStudentModel;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ExtractStudentModel
    @QueryShape GEOMETRY,
    @ParentLayerId BIGINT,
    @OutputLayerName NVARCHAR(256) = NULL,
    @TargetRows INT = NULL,
    @TargetCols INT = NULL,
    @OutputFormat NVARCHAR(50) = 'json',  -- 'json', 'safetensors', 'gguf'
    @ModelBlob VARBINARY(MAX) OUTPUT,
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        -- ==========================================================================================
        -- Phase 1: Validate inputs and retrieve parent layer metadata
        -- ==========================================================================================
        IF @QueryShape IS NULL OR @QueryShape.STIsEmpty() = 1
        BEGIN
            RAISERROR('Query shape cannot be null or empty.', 16, 1);
            RETURN;
        END

        IF @ParentLayerId IS NULL
        BEGIN
            RAISERROR('Parent layer ID cannot be null.', 16, 1);
            RETURN;
        END

        -- Retrieve parent layer dimensions if not specified
        IF @TargetRows IS NULL OR @TargetCols IS NULL
        BEGIN
            SELECT 
                @TargetRows = ISNULL(@TargetRows, ParameterCount),
                @TargetCols = ISNULL(@TargetCols, ParameterCount)
            FROM dbo.ModelLayers
            WHERE LayerId = @ParentLayerId;
        END

        IF @OutputLayerName IS NULL
            SET @OutputLayerName = 'student_layer_' + CAST(@ParentLayerId AS NVARCHAR(20));

        -- ==========================================================================================
        -- Phase 2: Query tensor atoms that intersect the shape
        -- ==========================================================================================
        DECLARE @IntersectingAtoms TABLE (
            TensorAtomId BIGINT,
            Coefficient FLOAT,
            SpatialSignature GEOMETRY
        );

        INSERT INTO @IntersectingAtoms (TensorAtomId, Coefficient, SpatialSignature)
        SELECT 
            ta.TensorAtomId,
            tac.Coefficient,
            ta.SpatialSignature
        FROM dbo.TensorAtom AS ta
        JOIN dbo.TensorAtomCoefficient AS tac ON ta.TensorAtomId = tac.TensorAtomId
        WHERE tac.ParentLayerId = @ParentLayerId 
          AND ta.SpatialSignature.STIntersects(@QueryShape) = 1
        ORDER BY tac.Coefficient DESC;  -- Prioritize high-importance components

        DECLARE @ComponentCount INT;
        SELECT @ComponentCount = COUNT(*) FROM @IntersectingAtoms;

        IF @ComponentCount = 0
        BEGIN
            RAISERROR('No tensor atoms found intersecting the query shape.', 16, 1);
            RETURN;
        END

        IF @Debug = 1
            PRINT 'Found ' + CAST(@ComponentCount AS NVARCHAR(20)) + ' intersecting tensor atoms.';

        -- ==========================================================================================
        -- Phase 3: Retrieve V^T vectors (payloads) and singular values
        -- ==========================================================================================
        DECLARE @UVectors TABLE (
            RowIndex INT IDENTITY(0,1),
            VectorJson NVARCHAR(MAX)
        );

        DECLARE @SValues TABLE (
            RowIndex INT IDENTITY(0,1),
            SingularValue FLOAT
        );

        DECLARE @VTMatrix TABLE (
            ComponentIndex INT IDENTITY(0,1),
            VectorJson NVARCHAR(MAX)
        );

        -- Retrieve payloads (V^T vectors) from FILESTREAM storage
        DECLARE @CurrentAtomId BIGINT;
        DECLARE @CurrentCoefficient FLOAT;
        DECLARE atom_cursor CURSOR FOR
            SELECT TensorAtomId, Coefficient FROM @IntersectingAtoms;

        OPEN atom_cursor;
        FETCH NEXT FROM atom_cursor INTO @CurrentAtomId, @CurrentCoefficient;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Get payload (V^T vector) from TensorAtomPayloads using CLR function
            DECLARE @payload VARBINARY(MAX);
            SET @payload = dbo.clr_GetTensorAtomPayload(@CurrentAtomId);

            IF @payload IS NOT NULL
            BEGIN
                -- Convert binary payload to JSON float array using CLR function
                DECLARE @vectorJson NVARCHAR(MAX);
                SET @vectorJson = dbo.clr_BytesToFloatArrayJson(@payload);
                
                INSERT INTO @VTMatrix (VectorJson) VALUES (@vectorJson);
                INSERT INTO @SValues (SingularValue) VALUES (@CurrentCoefficient);
            END

            FETCH NEXT FROM atom_cursor INTO @CurrentAtomId, @CurrentCoefficient;
        END;

        CLOSE atom_cursor;
        DEALLOCATE atom_cursor;

        -- ==========================================================================================
        -- Phase 4: Reconstruct weights using SVD synthesis (U * S * V^T)
        -- ==========================================================================================
        -- For student model synthesis, we use the CLR function to reconstruct
        DECLARE @synthesizedWeightsJson NVARCHAR(MAX);
        SET @synthesizedWeightsJson = dbo.clr_SynthesizeModelLayer(@QueryShape, @ParentLayerId);

        IF JSON_VALUE(@synthesizedWeightsJson, '$.error') IS NOT NULL
        BEGIN
            DECLARE @synthError NVARCHAR(MAX) = JSON_VALUE(@synthesizedWeightsJson, '$.error');
            RAISERROR('Student model synthesis failed: %s', 16, 1, @synthError);
            RETURN;
        END

        -- ==========================================================================================
        -- Phase 5: Serialize to requested output format
        -- ==========================================================================================
        IF @OutputFormat = 'json'
        BEGIN
            -- Return weights as JSON blob
            SET @ModelBlob = CAST(@synthesizedWeightsJson AS VARBINARY(MAX));
        END
        ELSE IF @OutputFormat = 'safetensors'
        BEGIN
            -- Future: Implement safetensors serialization via CLR
            -- For now, return JSON with format metadata
            DECLARE @safetensorsWrapper NVARCHAR(MAX) = JSON_MODIFY(
                JSON_MODIFY('{}', '$.format', 'safetensors'),
                '$.weights',
                JSON_QUERY(@synthesizedWeightsJson)
            );
            SET @ModelBlob = CAST(@safetensorsWrapper AS VARBINARY(MAX));
        END
        ELSE IF @OutputFormat = 'gguf'
        BEGIN
            -- Future: Implement GGUF serialization via CLR
            DECLARE @ggufWrapper NVARCHAR(MAX) = JSON_MODIFY(
                JSON_MODIFY('{}', '$.format', 'gguf'),
                '$.weights',
                JSON_QUERY(@synthesizedWeightsJson)
            );
            SET @ModelBlob = CAST(@ggufWrapper AS VARBINARY(MAX));
        END

        -- ==========================================================================================
        -- Phase 6: Return synthesis summary
        -- ==========================================================================================
        SELECT 
            @OutputLayerName AS LayerName,
            @ComponentCount AS ComponentsUsed,
            @TargetRows AS OutputRows,
            @TargetCols AS OutputCols,
            @OutputFormat AS OutputFormat,
            DATALENGTH(@ModelBlob) AS BlobSizeBytes,
            'SUCCESS' AS Status;

        IF @Debug = 1
            PRINT 'Student model extraction completed successfully.';

    END TRY
    BEGIN CATCH
        THROW;
    END CATCH;
END;
GO

PRINT 'Successfully created procedure dbo.sp_ExtractStudentModel.';
GO

USE Hartonomous;
GO

-- Drop the procedure if it already exists to ensure a clean slate.
IF OBJECT_ID('dbo.sp_AtomizeModel', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_AtomizeModel;
GO

CREATE OR ALTER PROCEDURE dbo.sp_AtomizeModel
    @model_blob VARBINARY(MAX),
    @model_format_hint NVARCHAR(50),
    @layer_name NVARCHAR(256),
    @parent_layer_id INT,
    @max_rank INT = 200,
    @debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        -- ==========================================================================================
        -- Phase 1: Extract Layer Weights & Perform SVD
        -- ==========================================================================================
        DECLARE @layer_weights_json NVARCHAR(MAX);
        SET @layer_weights_json = dbo.clr_ParseModelLayer(@model_blob, @layer_name, @model_format_hint);

        IF @layer_weights_json IS NULL OR JSON_VALUE(@layer_weights_json, '$.error') IS NOT NULL
        BEGIN
            DECLARE @parseError NVARCHAR(MAX) = ISNULL(JSON_VALUE(@layer_weights_json, '$.error'), 'Parsing returned null.');
            RAISERROR('Failed to parse model layer: %s', 16, 1, @parseError);
            RETURN;
        END

        DECLARE @rows INT, @cols INT;
        SELECT @rows = NeuronCount, @cols = NeuronCount -- Assuming square matrix for now
        FROM dbo.ModelLayers
        WHERE LayerId = @parent_layer_id;

        IF @rows IS NULL OR @cols IS NULL
        BEGIN
            RAISERROR('Could not determine layer dimensions from dbo.ModelLayers for LayerId: %d', 16, 1, @parent_layer_id);
            RETURN;
        END

        DECLARE @svd_result_json NVARCHAR(MAX);
        SET @svd_result_json = dbo.clr_SvdDecompose(@layer_weights_json, @rows, @cols, @max_rank);

        IF @svd_result_json IS NULL OR JSON_VALUE(@svd_result_json, '$.error') IS NOT NULL
        BEGIN
            DECLARE @svdError NVARCHAR(MAX) = ISNULL(JSON_VALUE(@svd_result_json, '$.error'), 'SVD returned null.');
            RAISERROR('SVD decomposition failed: %s', 16, 1, @svdError);
            RETURN;
        END

        -- ==========================================================================================
        -- Phase 2: Shred SVD results, Project to 3D Space, and Fuse Importance
        -- ==========================================================================================
        DECLARE @AtomComponents TABLE (
            ComponentIndex INT PRIMARY KEY,
            U_Vector NVARCHAR(MAX),
            S_Value FLOAT,
            VT_Vector_Json NVARCHAR(MAX),
            SpatialSignature GEOMETRY
        );

        INSERT INTO @AtomComponents (ComponentIndex, U_Vector, S_Value, VT_Vector_Json)
        SELECT
            CAST(u.[key] AS INT),
            u.value,
            CAST(s.value AS FLOAT),
            vt.value
        FROM OPENJSON(@svd_result_json, '$.U') AS u
        JOIN OPENJSON(@svd_result_json, '$.S') AS s ON u.[key] = s.[key]
        JOIN OPENJSON(@svd_result_json, '$.VT') AS vt ON u.[key] = vt.[key];

        UPDATE @AtomComponents
        SET SpatialSignature = geometry::STPointFromText(
                dbo.clr_CreateGeometryPointWithImportance(
                    JSON_VALUE(dbo.clr_ProjectToPoint(U_Vector), '$.X'),
                    JSON_VALUE(dbo.clr_ProjectToPoint(U_Vector), '$.Y'),
                    JSON_VALUE(dbo.clr_ProjectToPoint(U_Vector), '$.Z'),
                    S_Value
                ), 4326
            );

        -- ==========================================================================================
        -- Phase 3: Store Atoms and Payloads
        -- ==========================================================================================
        DECLARE @NewTensorAtoms TABLE (
            TensorAtomId BIGINT,
            ComponentIndex INT
        );

        -- Insert the new TensorAtoms with their spatial signatures.
        INSERT INTO dbo.TensorAtom (SpatialSignature, AtomType)
        OUTPUT inserted.TensorAtomId, ac.ComponentIndex INTO @NewTensorAtoms
        SELECT ac.SpatialSignature, 'SVD_Component'
        FROM @AtomComponents ac;

        -- Store the corresponding V-vectors as FILESTREAM payloads.
        DECLARE @CurrentAtomId BIGINT, @CurrentComponentIndex INT;
        DECLARE atom_cursor CURSOR FOR
            SELECT TensorAtomId, ComponentIndex FROM @NewTensorAtoms;

        OPEN atom_cursor;
        FETCH NEXT FROM atom_cursor INTO @CurrentAtomId, @CurrentComponentIndex;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            DECLARE @vt_vector_json NVARCHAR(MAX);
            SELECT @vt_vector_json = VT_Vector_Json
            FROM @AtomComponents
            WHERE ComponentIndex = @CurrentComponentIndex;

            -- Convert the JSON array of floats to VARBINARY for storage.
            DECLARE @payload VARBINARY(MAX) = dbo.clr_JsonFloatArrayToBytes(@vt_vector_json);

            -- Call the CLR procedure to store the payload.
            EXEC dbo.clr_StoreTensorAtomPayload @CurrentAtomId, @payload;

            FETCH NEXT FROM atom_cursor INTO @CurrentAtomId, @CurrentComponentIndex;
        END;

        CLOSE atom_cursor;
        DEALLOCATE atom_cursor;

        -- ==========================================================================================
        -- Phase 4: Link Atoms to Parent Layer
        -- ==========================================================================================
        INSERT INTO dbo.TensorAtomCoefficient (ParentLayerId, TensorAtomId, Coefficient)
        SELECT
            @parent_layer_id,
            nta.TensorAtomId,
            ac.S_Value
        FROM @NewTensorAtoms nta
        JOIN @AtomComponents ac ON nta.ComponentIndex = ac.ComponentIndex;

        IF @debug = 1
        BEGIN
            PRINT 'Successfully atomized model layer ' + CAST(@parent_layer_id AS NVARCHAR(10)) +
                  ' into ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' tensor atoms.';
        END

    END TRY
    BEGIN CATCH
        -- Rethrow the error to be caught by the caller.
        THROW;
    END CATCH;
END;
GO

PRINT 'Successfully created procedure dbo.sp_AtomizeModel.';
GO
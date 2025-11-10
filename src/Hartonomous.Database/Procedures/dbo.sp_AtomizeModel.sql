-- Drop the procedure if it already exists to ensure a clean slate.
IF OBJECT_ID('dbo.sp_AtomizeModel', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_AtomizeModel;

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

        SET @layer_weights_json = dbo.clr_ParseModelLayer(@model_blob, @layer_name, @model_format_hint);

        IF @layer_weights_json IS NULL OR JSON_VALUE(@layer_weights_json, '$.error') IS NOT NULL
        BEGIN

            RAISERROR('Failed to parse model layer: %s', 16, 1, @parseError);
            RETURN;
        END

        SELECT @rows = NeuronCount, @cols = NeuronCount -- Assuming square matrix for now
        FROM dbo.ModelLayers
        WHERE LayerId = @parent_layer_id;

        IF @rows IS NULL OR @cols IS NULL
        BEGIN
            RAISERROR('Could not determine layer dimensions from dbo.ModelLayers for LayerId: %d', 16, 1, @parent_layer_id);
            RETURN;
        END

        SET @svd_result_json = dbo.clr_SvdDecompose(@layer_weights_json, @rows, @cols, @max_rank);

        IF @svd_result_json IS NULL OR JSON_VALUE(@svd_result_json, '$.error') IS NOT NULL
        BEGIN

            RAISERROR('SVD decomposition failed: %s', 16, 1, @svdError);
            RETURN;
        END

        -- ==========================================================================================
        -- Phase 2: Shred SVD results, Project to 3D Space, and Fuse Importance
        -- ==========================================================================================

            ComponentIndex INT PRIMARY KEY,
            U_Vector NVARCHAR(MAX),
            S_Value FLOAT,
            VT_Vector_Json NVARCHAR(MAX),
            SpatialSignature GEOMETRY
        );

        

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

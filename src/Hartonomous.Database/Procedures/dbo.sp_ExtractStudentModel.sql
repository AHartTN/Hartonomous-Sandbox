-- sp_ExtractStudentModel: Student Model Factory - Shape-to-Model Synthesis
-- Queries tensor atoms by spatial shape, retrieves SVD components, synthesizes new model layer

IF OBJECT_ID('dbo.sp_ExtractStudentModel', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_ExtractStudentModel;

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
                @TargetRows = ISNULL(@TargetRows, NeuronCount),
                @TargetCols = ISNULL(@TargetCols, NeuronCount)
            FROM dbo.ModelLayers
            WHERE LayerId = @ParentLayerId;
        END

        IF @OutputLayerName IS NULL
            SET @OutputLayerName = 'student_layer_' + CAST(@ParentLayerId AS NVARCHAR(20));

        -- ==========================================================================================
        -- Phase 2: Query tensor atoms that intersect the shape
        -- ==========================================================================================

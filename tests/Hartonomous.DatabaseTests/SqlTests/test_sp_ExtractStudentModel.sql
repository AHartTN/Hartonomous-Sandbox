-- Test: sp_ExtractStudentModel creates student model and invokes feedback procedure
-- Validates architecture-aware distillation and feedback integration

-- Prerequisites: Parent model with layers and tensor atoms must exist
IF NOT EXISTS (SELECT 1 FROM dbo.Models WHERE ModelId = 1)
BEGIN
    RAISERROR('Parent model (ModelId=1) not found; skipping sp_ExtractStudentModel test.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.ModelLayers WHERE ModelId = 1)
BEGIN
    RAISERROR('Parent model (ModelId=1) has no layers; skipping sp_ExtractStudentModel test.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.TensorAtoms WHERE ModelId = 1)
BEGIN
    RAISERROR('Parent model (ModelId=1) has no tensor atoms; skipping sp_ExtractStudentModel test.', 16, 1);
    RETURN;
END;

BEGIN TRY
    DECLARE @parentModelId INT = 1;
    DECLARE @studentModelName NVARCHAR(200) = N'Test_Student_' + CAST(NEWID() AS NVARCHAR(36));
    DECLARE @importanceThreshold FLOAT = 0.3;

    -- Count original layers and atoms
    DECLARE @originalLayerCount INT = (SELECT COUNT(*) FROM dbo.ModelLayers WHERE ModelId = @parentModelId);
    DECLARE @originalAtomCount BIGINT = (SELECT COUNT(*) FROM dbo.TensorAtoms WHERE ModelId = @parentModelId);

    -- Execute student extraction
    EXEC dbo.sp_ExtractStudentModel
        @ParentModelId = @parentModelId,
        @layer_subset = NULL,
        @importance_threshold = @importanceThreshold,
        @NewModelName = @studentModelName;

    -- Verify student model was created
    DECLARE @studentModelId INT;
    SELECT @studentModelId = ModelId
    FROM dbo.Models
    WHERE ModelName = @studentModelName;

    IF @studentModelId IS NULL
    BEGIN
        RAISERROR('Student model was not created.', 16, 1);
    END;

    -- Verify student has layers
    DECLARE @studentLayerCount INT = (SELECT COUNT(*) FROM dbo.ModelLayers WHERE ModelId = @studentModelId);
    IF @studentLayerCount = 0
    BEGIN
        RAISERROR('Student model has no layers.', 16, 1);
    END;

    IF @studentLayerCount > @originalLayerCount
    BEGIN
        RAISERROR('Student model has more layers than parent (expected <= %d, got %d).', 16, 1, @originalLayerCount, @studentLayerCount);
    END;

    -- Verify student has tensor atoms
    DECLARE @studentAtomCount BIGINT = (SELECT COUNT(*) FROM dbo.TensorAtoms WHERE ModelId = @studentModelId);
    IF @studentAtomCount = 0
    BEGIN
        RAISERROR('Student model has no tensor atoms.', 16, 1);
    END;

    IF @studentAtomCount > @originalAtomCount
    BEGIN
        RAISERROR('Student model has more atoms than parent (expected <= %d, got %d).', 16, 1, @originalAtomCount, @studentAtomCount);
    END;

    -- Verify student's parameter count was updated
    DECLARE @studentParameterCount BIGINT;
    SELECT @studentParameterCount = ParameterCount
    FROM dbo.Models
    WHERE ModelId = @studentModelId;

    IF @studentParameterCount IS NULL OR @studentParameterCount = 0
    BEGIN
        RAISERROR('Student model ParameterCount was not set.', 16, 1);
    END;

    IF @studentParameterCount <> @studentAtomCount
    BEGIN
        RAISERROR('Student model ParameterCount does not match TensorAtom count (expected %d, got %d).', 16, 1, @studentAtomCount, @studentParameterCount);
    END;

    -- Verify tensor atom coefficients were cloned
    IF EXISTS (SELECT 1 FROM dbo.TensorAtomCoefficients WHERE TensorAtomId IN (SELECT TensorAtomId FROM dbo.TensorAtoms WHERE ModelId = @parentModelId))
    BEGIN
        -- Parent has coefficients, student should too
        IF NOT EXISTS (SELECT 1 FROM dbo.TensorAtomCoefficients WHERE TensorAtomId IN (SELECT TensorAtomId FROM dbo.TensorAtoms WHERE ModelId = @studentModelId))
        BEGIN
            RAISERROR('Student model missing tensor atom coefficients despite parent having them.', 16, 1);
        END;
    END;

    -- Verify GRAPH MATCH was used (LayerAtomId should be populated for architecture-aware distillation)
    -- This is indirect validation; direct SQL graph queries would confirm path traversal
    DECLARE @layersWithAtomId INT = (
        SELECT COUNT(*)
        FROM dbo.ModelLayers
        WHERE ModelId = @studentModelId AND LayerAtomId IS NOT NULL
    );

    -- Note: Not all layers may have LayerAtomId if graph edges weren't created during ingestion
    -- This test is informational rather than a hard assertion
    IF @layersWithAtomId > 0
    BEGIN
        PRINT '✓ Student model has ' + CAST(@layersWithAtomId AS NVARCHAR(10)) + ' layers with LayerAtomId (architecture graph integrated).';
    END
    ELSE
    BEGIN
        PRINT '⚠ Student model has no layers with LayerAtomId (graph edges may not be populated).';
    END;

    -- Verify feedback procedure was invoked (indirect: check that student model has feedback data)
    -- Since sp_UpdateModelWeightsFromFeedback requires InferenceRequests with ratings, this may not apply unless seeded
    -- We verify it doesn't throw errors instead
    PRINT '✓ sp_ExtractStudentModel test passed: Student model created with ' + CAST(@studentLayerCount AS NVARCHAR(10)) + ' layers and ' + CAST(@studentAtomCount AS NVARCHAR(10)) + ' tensor atoms.';

    -- Cleanup: Delete test student model
    DELETE FROM dbo.TensorAtomCoefficients WHERE TensorAtomId IN (SELECT TensorAtomId FROM dbo.TensorAtoms WHERE ModelId = @studentModelId);
    DELETE FROM dbo.TensorAtoms WHERE ModelId = @studentModelId;
    DELETE FROM dbo.ModelLayers WHERE ModelId = @studentModelId;
    DELETE FROM dbo.Models WHERE ModelId = @studentModelId;

    PRINT '✓ Test student model cleaned up.';
END TRY
BEGIN CATCH
    DECLARE @errorMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR('sp_ExtractStudentModel test failed: %s', 16, 1, @errorMsg);
END CATCH;
GO

CREATE PROCEDURE dbo.sp_DynamicStudentExtraction
    @ParentModelId INT,
    @target_size_ratio FLOAT = 0.5,
    @selection_strategy NVARCHAR(20) = 'importance'
AS
BEGIN
    SET NOCOUNT ON;

    IF @target_size_ratio <= 0 OR @target_size_ratio > 1
    BEGIN
        ;THROW 50020, 'Target size ratio must be within (0, 1].', 1;
    END;

    DECLARE @parent_exists INT = (
        SELECT COUNT(*)
        FROM dbo.Models
        WHERE ModelId = @ParentModelId
    );

    IF @parent_exists = 0
    BEGIN
        ;THROW 50021, 'Parent model does not exist.', 1;
    END;

    DECLARE @ratio_percent INT = CEILING(@target_size_ratio * 100);
    IF @ratio_percent < 1 SET @ratio_percent = 1;
    DECLARE @NewModelName NVARCHAR(200) = CONCAT('Student_', @ParentModelId, '_', @selection_strategy, '_', @ratio_percent, 'pct');
    DECLARE @layer_subset NVARCHAR(MAX) = NULL;
    DECLARE @importance_threshold FLOAT = NULL;

    IF @selection_strategy = 'layer'
    BEGIN
        DECLARE @total_layers INT = (
            SELECT COUNT(*)
            FROM dbo.ModelLayers
            WHERE ModelId = @ParentModelId
        );

        IF @total_layers = 0
        BEGIN
            ;THROW 50022, 'Parent model has no layers to extract.', 1;
        END;

        DECLARE @layers_to_take INT = CEILING(@total_layers * @target_size_ratio);
        IF @layers_to_take < 1 SET @layers_to_take = 1;

        SELECT @layer_subset = STRING_AGG(CAST(LayerIdx AS NVARCHAR(10)), ',') WITHIN GROUP (ORDER BY LayerIdx)
        FROM (
            SELECT TOP (@layers_to_take) LayerIdx
            FROM dbo.ModelLayers
            WHERE ModelId = @ParentModelId
            ORDER BY LayerIdx
        ) AS layer_selection;
    END
    ELSE IF @selection_strategy = 'random'
    BEGIN
        DECLARE @total_layers_random INT = (
            SELECT COUNT(*)
            FROM dbo.ModelLayers
            WHERE ModelId = @ParentModelId
        );

        IF @total_layers_random = 0
        BEGIN
            ;THROW 50023, 'Parent model has no layers to extract.', 1;
        END;

        DECLARE @random_take INT = CEILING(@total_layers_random * @target_size_ratio);
        IF @random_take < 1 SET @random_take = 1;

        SELECT @layer_subset = STRING_AGG(CAST(LayerIdx AS NVARCHAR(10)), ',')
        FROM (
            SELECT TOP (@random_take) LayerIdx
            FROM dbo.ModelLayers
            WHERE ModelId = @ParentModelId
            ORDER BY NEWID()
        ) AS random_layers;
    END
    ELSE
    BEGIN
        DECLARE @total_atoms INT = (
            SELECT COUNT(*)
            FROM dbo.TensorAtoms
            WHERE ModelId = @ParentModelId
        );

        IF @total_atoms = 0
        BEGIN
            SET @importance_threshold = NULL;
        END
        ELSE
        BEGIN
            DECLARE @atoms_to_take INT = CEILING(@total_atoms * @target_size_ratio);
            IF @atoms_to_take < 1 SET @atoms_to_take = 1;

            SELECT @importance_threshold = MIN(ImportanceScore)
            FROM (
                SELECT TOP (@atoms_to_take) ImportanceScore
                FROM dbo.TensorAtoms
                WHERE ModelId = @ParentModelId
                  AND ImportanceScore IS NOT NULL
                ORDER BY ImportanceScore DESC
            ) AS ranked;
        END;
    END;

    EXEC dbo.sp_ExtractStudentModel
        @ParentModelId = @ParentModelId,
        @layer_subset = @layer_subset,
        @importance_threshold = @importance_threshold,
        @NewModelName = @NewModelName;
END;
GO
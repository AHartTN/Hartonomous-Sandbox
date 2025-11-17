CREATE FUNCTION dbo.fn_SelectModelsForTask
(
    @task_type NVARCHAR(50) = NULL,
    @model_ids NVARCHAR(MAX) = NULL,
    @weights_json NVARCHAR(MAX) = NULL,
    @required_modalities NVARCHAR(MAX) = NULL,
    @additional_model_types NVARCHAR(MAX) = NULL
)
RETURNS @models TABLE
(
    ModelId INT PRIMARY KEY,
    Weight FLOAT NOT NULL,
    ModelName NVARCHAR(200) NULL
)
AS
BEGIN
    DECLARE @trimmedIds NVARCHAR(MAX) = NULLIF(LTRIM(RTRIM(@model_ids)), '');
    DECLARE @normalizedTask NVARCHAR(50) = NULLIF(LTRIM(RTRIM(@task_type)), '');
    DECLARE @normalizedModality NVARCHAR(50) = NULL;
    DECLARE @explicit BIT = 0;

    IF @trimmedIds IS NOT NULL
    BEGIN
        INSERT INTO @models (ModelId, Weight, ModelName)
        SELECT DISTINCT
            m.ModelId,
            1.0,
            m.ModelName
        FROM STRING_SPLIT(@trimmedIds, ',') vals
        CROSS APPLY (SELECT TRY_CAST(LTRIM(RTRIM(vals.value)) AS INT) AS ParsedId) parsed
        INNER JOIN dbo.Models m ON m.ModelId = parsed.ParsedId
        WHERE parsed.ParsedId IS NOT NULL;

        IF EXISTS (SELECT 1 FROM @models)
        BEGIN
            SET @explicit = 1;
        END;
    END;

    DECLARE @modalities TABLE (Modality NVARCHAR(100) PRIMARY KEY);
    INSERT INTO @modalities (Modality)
    SELECT DISTINCT NULLIF(LTRIM(RTRIM(value)), '')
    FROM STRING_SPLIT(COALESCE(@required_modalities, ''), ',')
    WHERE NULLIF(LTRIM(RTRIM(value)), '') IS NOT NULL;

    IF EXISTS (SELECT 1 FROM @modalities)
    BEGIN
        SELECT TOP (1) @normalizedModality = Modality FROM @modalities ORDER BY Modality;
    END;

    IF @explicit = 0
    BEGIN
        DECLARE @additionalTypes TABLE (ModelType NVARCHAR(100) PRIMARY KEY);
        INSERT INTO @additionalTypes (ModelType)
        SELECT DISTINCT NULLIF(LTRIM(RTRIM(value)), '')
        FROM STRING_SPLIT(COALESCE(@additional_model_types, ''), ',')
        WHERE NULLIF(LTRIM(RTRIM(value)), '') IS NOT NULL;

        INSERT INTO @models (ModelId, Weight, ModelName)
        SELECT DISTINCT
            m.ModelId,
            COALESCE(
                CASE
                    WHEN @normalizedTask IS NOT NULL
                        THEN TRY_CAST(JSON_VALUE(m.Config, CONCAT('$.weights.', @normalizedTask)) AS FLOAT)
                    ELSE NULL
                END,
                1.0
            ) AS Weight,
            m.ModelName
        FROM dbo.Models m
        LEFT JOIN dbo.ModelMetadata md ON md.ModelId = m.ModelId
        LEFT JOIN @additionalTypes at ON at.ModelType = m.ModelType
        WHERE
            m.ModelType IN ('multimodal', 'general')
            OR (@normalizedTask IS NOT NULL AND m.ModelType = @normalizedTask)
            OR at.ModelType IS NOT NULL
            OR (
                @normalizedTask IS NOT NULL
                AND md.SupportedTasks IS NOT NULL
                AND ISJSON(md.SupportedTasks) = 1
                AND EXISTS (
                    SELECT 1 FROM OPENJSON(md.SupportedTasks) WHERE value = @normalizedTask
                )
            )
            OR (
                EXISTS (SELECT 1 FROM @modalities)
                AND md.SupportedModalities IS NOT NULL
                AND ISJSON(md.SupportedModalities) = 1
                AND EXISTS (
                    SELECT 1
                    FROM OPENJSON(md.SupportedModalities)
                    WHERE value IN (SELECT Modality FROM @modalities)
                )
            );
    END;

    IF NOT EXISTS (SELECT 1 FROM @models)
        RETURN;

    DECLARE @weights NVARCHAR(MAX) = NULLIF(@weights_json, 'null');

    IF @weights IS NOT NULL AND ISJSON(@weights) = 1
    BEGIN
        WITH WeightOverrides AS (
            SELECT
                TRY_CAST(JSON_VALUE(value, '$.modelId') AS INT) AS ModelId,
                TRY_CAST(JSON_VALUE(value, '$.weight') AS FLOAT) AS Weight
            FROM OPENJSON(@weights)
            WHERE JSON_VALUE(value, '$.modelId') IS NOT NULL
        )
        UPDATE m
        SET Weight = CASE WHEN w.Weight IS NOT NULL AND w.Weight > 0 THEN w.Weight ELSE m.Weight END
        FROM @models m
        INNER JOIN WeightOverrides w ON w.ModelId = m.ModelId;
    END;

    DECLARE @total FLOAT = (SELECT SUM(Weight) FROM @models);

    IF @total IS NULL OR @total = 0
    BEGIN
        UPDATE @models SET Weight = 1.0;
        SET @total = (SELECT SUM(Weight) FROM @models);
    END;

    UPDATE @models SET Weight = Weight / @total;

    RETURN;
END;
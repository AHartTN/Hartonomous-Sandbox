CREATE PROCEDURE dbo.sp_ExtractStudentModel
    @ParentModelId INT,
    @layer_subset NVARCHAR(MAX) = NULL,
    @importance_threshold FLOAT = 0.5,
    @NewModelName NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @startedTransaction BIT = 0;

    IF @@TRANCOUNT = 0
    BEGIN
        SET @startedTransaction = 1;
        BEGIN TRANSACTION;
    END
    ELSE
    BEGIN
        SAVE TRANSACTION ExtractStudentModelSavepoint;
    END;

    BEGIN TRY
        PRINT 'EXTRACTING STUDENT MODEL via T-SQL SELECT';
        PRINT 'Distillation stays inside SQL Server atom substrate.';

        DECLARE @ParentModelType NVARCHAR(100);
        DECLARE @parent_architecture NVARCHAR(100);
        DECLARE @parent_config NVARCHAR(MAX);  -- Changed from JSON to avoid conversion errors
        DECLARE @parent_parameter_count BIGINT;

        SELECT
            @ParentModelType = ModelType,
            @parent_architecture = Architecture,
            @parent_config = CAST(Config AS NVARCHAR(MAX)),  -- Explicit cast from JSON
            @parent_parameter_count = ParameterCount
        FROM dbo.Models
        WHERE ModelId = @ParentModelId;

        IF @ParentModelType IS NULL
        BEGIN
            ;THROW 50001, 'Parent model not found.', 1;
        END;

        INSERT INTO dbo.Models (ModelName, ModelType, Architecture, Config, ParameterCount)
        VALUES (@NewModelName, @ParentModelType, @parent_architecture, @parent_config, @parent_parameter_count);

        DECLARE @StudentModelId INT = SCOPE_IDENTITY();

        DECLARE @SelectedLayers TABLE (LayerIdx INT PRIMARY KEY);
        DECLARE @has_filter BIT = 0;

        DECLARE @GraphLayerOrder TABLE
        (
            LayerId BIGINT PRIMARY KEY,
            Sequence INT NOT NULL
        );

        DECLARE @RootLayerId BIGINT;
        DECLARE @RootAtomId BIGINT;
        DECLARE @ArchitectureRelationType NVARCHAR(128) = N'architecture.successor';

        IF @layer_subset IS NOT NULL AND LTRIM(RTRIM(@layer_subset)) <> ''
        BEGIN
            SET @has_filter = 1;

            INSERT INTO @SelectedLayers (LayerIdx)
            SELECT DISTINCT TRY_CAST(value AS INT)
            FROM STRING_SPLIT(@layer_subset, ',')
            WHERE TRY_CAST(value AS INT) IS NOT NULL;
        END;

        SELECT TOP (1)
            @RootLayerId = ml.LayerId,
            @RootAtomId = ml.LayerAtomId
        FROM dbo.ModelLayers AS ml
        WHERE ml.ModelId = @ParentModelId
        ORDER BY ml.LayerIdx;

        IF @RootAtomId IS NOT NULL
        BEGIN
            -- Use layer index as sequence when graph traversal from root
            -- Project FOR PATH results then join with ModelLayers
            INSERT INTO @GraphLayerOrder (LayerId, Sequence)
            SELECT DISTINCT
                ml.LayerId,
                ml.LayerIdx
            FROM (
                SELECT
                    LAST_VALUE(destNode.AtomId) WITHIN GROUP (GRAPH PATH) AS LastAtomId
                FROM graph.AtomGraphNodes AS startNode,
                     graph.AtomGraphEdges FOR PATH AS path,
                     graph.AtomGraphNodes FOR PATH AS destNode
                WHERE MATCH(SHORTEST_PATH(startNode(-(path)->destNode)+))
                  AND startNode.AtomId = @RootAtomId
            ) AS GraphPaths
            INNER JOIN dbo.ModelLayers AS ml ON ml.LayerAtomId = GraphPaths.LastAtomId
            WHERE ml.ModelId = @ParentModelId;
        END;

        IF @RootLayerId IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM @GraphLayerOrder WHERE LayerId = @RootLayerId)
        BEGIN
            INSERT INTO @GraphLayerOrder (LayerId, Sequence)
            VALUES (@RootLayerId, 0);
        END;

        IF EXISTS (SELECT 1 FROM @GraphLayerOrder)
        BEGIN
            INSERT INTO @GraphLayerOrder (LayerId, Sequence)
            SELECT ml.LayerId, ml.LayerIdx
            FROM dbo.ModelLayers AS ml
            WHERE ml.ModelId = @ParentModelId
              AND NOT EXISTS (SELECT 1 FROM @GraphLayerOrder AS existing WHERE existing.LayerId = ml.LayerId);
        END
        ELSE
        BEGIN
            INSERT INTO @GraphLayerOrder (LayerId, Sequence)
            SELECT ml.LayerId, ml.LayerIdx
            FROM dbo.ModelLayers AS ml
            WHERE ml.ModelId = @ParentModelId;
        END;

        DECLARE @LayerData TABLE
        (
            LayerId BIGINT PRIMARY KEY,
            LayerIdx INT NOT NULL,
            LayerName NVARCHAR(100) NULL,
            LayerType NVARCHAR(50) NULL,
            WeightsGeometry GEOMETRY NULL,
            TensorShape NVARCHAR(MAX) NULL,
            TensorDtype NVARCHAR(20) NULL,
            QuantizationType NVARCHAR(20) NULL,
            QuantizationScale FLOAT NULL,
            QuantizationZeroPoint FLOAT NULL,
            Parameters NVARCHAR(MAX) NULL,  -- Changed from JSON to avoid conversion errors
            ParameterCount BIGINT NULL,
            CacheHitRate FLOAT NULL,
            AvgComputeTimeMs FLOAT NULL
        );

                INSERT INTO @LayerData
                SELECT
                        ml.LayerId,
                        ml.LayerIdx,
                        ml.LayerName,
                        ml.LayerType,
                        ml.WeightsGeometry,
                        ml.TensorShape,
                        ml.TensorDtype,
                        ml.QuantizationType,
                        ml.QuantizationScale,
                        ml.QuantizationZeroPoint,
                        CAST(ml.Parameters AS NVARCHAR(MAX)),  -- Explicit cast from JSON
                        ml.ParameterCount,
                        ml.CacheHitRate,
                        ml.AvgComputeTimeMs
                        FROM dbo.ModelLayers AS ml
                        INNER JOIN @GraphLayerOrder AS gl ON gl.LayerId = ml.LayerId
                        WHERE ml.ModelId = @ParentModelId
                            AND (
                                @has_filter = 0
                                OR EXISTS (SELECT 1 FROM @SelectedLayers AS sl WHERE sl.LayerIdx = ml.LayerIdx)
                            );

        DECLARE @LayerMap TABLE
        (
            OldLayerId BIGINT PRIMARY KEY,
            NewLayerId BIGINT NOT NULL
        );

        MERGE dbo.ModelLayers AS target
        USING (
            SELECT * FROM @LayerData
        ) AS src
            ON 1 = 0
        WHEN NOT MATCHED THEN
            INSERT (
                ModelId,
                LayerIdx,
                LayerName,
                LayerType,
                WeightsGeometry,
                TensorShape,
                TensorDtype,
                QuantizationType,
                QuantizationScale,
                QuantizationZeroPoint,
                Parameters,
                ParameterCount,
                CacheHitRate,
                AvgComputeTimeMs
            )
            VALUES (
                @StudentModelId,
                src.LayerIdx,
                src.LayerName,
                src.LayerType,
                src.WeightsGeometry,
                src.TensorShape,
                src.TensorDtype,
                src.QuantizationType,
                src.QuantizationScale,
                src.QuantizationZeroPoint,
                src.Parameters,
                src.ParameterCount,
                src.CacheHitRate,
                src.AvgComputeTimeMs
            )
        OUTPUT src.LayerId, inserted.LayerId INTO @LayerMap (OldLayerId, NewLayerId);

        DECLARE @TensorAtomData TABLE
        (
            TensorAtomId BIGINT PRIMARY KEY,
            LayerId BIGINT NOT NULL,
            AtomId BIGINT NOT NULL,
            AtomType NVARCHAR(128) NOT NULL,
            SpatialSignature GEOMETRY NULL,
            GeometryFootprint GEOMETRY NULL,
            Metadata NVARCHAR(MAX) NULL,  -- Changed from JSON to avoid conversion errors
            ImportanceScore REAL NULL
        );

        INSERT INTO @TensorAtomData
        SELECT
            ta.TensorAtomId,
            ta.LayerId,
            ta.AtomId,
            ta.AtomType,
            ta.SpatialSignature,
            ta.GeometryFootprint,
            CAST(ta.Metadata AS NVARCHAR(MAX)),  -- Explicit cast from JSON
            ta.ImportanceScore
        FROM dbo.TensorAtoms AS ta
        WHERE ta.ModelId = @ParentModelId
          AND ta.LayerId IN (SELECT OldLayerId FROM @LayerMap)
          AND (@importance_threshold IS NULL
               OR ta.ImportanceScore IS NULL
               OR ta.ImportanceScore >= @importance_threshold);

        DECLARE @TensorMap TABLE
        (
            OldTensorAtomId BIGINT PRIMARY KEY,
            NewTensorAtomId BIGINT NOT NULL
        );

        MERGE dbo.TensorAtoms AS target
        USING (
            SELECT
                src.TensorAtomId,
                src.AtomId,
                src.AtomType,
                src.SpatialSignature,
                src.GeometryFootprint,
                src.Metadata,
                src.ImportanceScore,
                lm.NewLayerId
            FROM @TensorAtomData AS src
            INNER JOIN @LayerMap AS lm ON lm.OldLayerId = src.LayerId
        ) AS src
            ON 1 = 0
        WHEN NOT MATCHED THEN
            INSERT (
                AtomId,
                ModelId,
                LayerId,
                AtomType,
                SpatialSignature,
                GeometryFootprint,
                Metadata,
                ImportanceScore
            )
            VALUES (
                src.AtomId,
                @StudentModelId,
                src.NewLayerId,
                src.AtomType,
                src.SpatialSignature,
                src.GeometryFootprint,
                src.Metadata,
                src.ImportanceScore
            )
        OUTPUT src.TensorAtomId, inserted.TensorAtomId INTO @TensorMap (OldTensorAtomId, NewTensorAtomId);

        DECLARE @CoeffData TABLE
        (
            TensorAtomId BIGINT NOT NULL,
            ParentLayerId BIGINT NOT NULL,
            TensorRole NVARCHAR(128) NULL,
            Coefficient REAL NOT NULL
        );

        INSERT INTO @CoeffData
        SELECT
            coeff.TensorAtomId,
            coeff.ParentLayerId,
            coeff.TensorRole,
            coeff.Coefficient
        FROM dbo.TensorAtomCoefficients AS coeff
        WHERE coeff.TensorAtomId IN (SELECT OldTensorAtomId FROM @TensorMap);

        INSERT INTO dbo.TensorAtomCoefficients
        (
            TensorAtomId,
            ParentLayerId,
            TensorRole,
            Coefficient
        )
        SELECT
            tm.NewTensorAtomId,
            lm.NewLayerId,
            cd.TensorRole,
            cd.Coefficient
        FROM @CoeffData AS cd
        INNER JOIN @TensorMap AS tm ON tm.OldTensorAtomId = cd.TensorAtomId
        INNER JOIN @LayerMap AS lm ON lm.OldLayerId = cd.ParentLayerId;

        DECLARE @original_atoms BIGINT = (
            SELECT COUNT(*)
            FROM dbo.TensorAtoms
            WHERE ModelId = @ParentModelId
        );

        DECLARE @student_atoms BIGINT = (
            SELECT COUNT(*)
            FROM dbo.TensorAtoms
            WHERE ModelId = @StudentModelId
        );

        UPDATE dbo.Models
        SET ParameterCount = @student_atoms,
            LastUsed = NULL,
            UsageCount = 0
        WHERE ModelId = @StudentModelId;

        PRINT 'Student model pruned. Now executing fine-tuning pass...';
        EXEC dbo.sp_UpdateModelWeightsFromFeedback
            @ModelId = @StudentModelId,
            @learningRate = 0.01,
            @minRatings = 1
        WITH RESULT SETS NONE;

        SELECT
            @StudentModelId AS student_model_id,
            @NewModelName AS student_name,
            @original_atoms AS original_tensor_atoms,
            @student_atoms AS student_tensor_atoms,
            CASE
                WHEN @original_atoms = 0 THEN NULL
                ELSE CAST(100.0 * @student_atoms / @original_atoms AS DECIMAL(6, 2))
            END AS atom_retention_percent
        OPTION (MAXDOP 1);

        PRINT 'Student model extracted: tensor atoms and coefficients cloned.';

        IF @startedTransaction = 1
        BEGIN
            COMMIT TRANSACTION;
        END;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() = -1
        BEGIN
            ROLLBACK TRANSACTION;
        END
        ELSE IF XACT_STATE() = 1
        BEGIN
            IF @startedTransaction = 1
            BEGIN
                ROLLBACK TRANSACTION;
            END
            ELSE
            BEGIN
                ROLLBACK TRANSACTION ExtractStudentModelSavepoint;
            END
        END;

        THROW;
    END CATCH;
END;
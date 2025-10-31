-- =============================================-- =============================================USE Hartonomous;

-- Atomized Model Ingestion Sample (MiniNN-V1)

-- =============================================-- Atomized Model Ingestion Sample (MiniNN-V1)GO

USE Hartonomous;

GO-- =============================================



PRINT '============================================================';USE Hartonomous;

PRINT 'ATOMIZED MODEL INGESTION SAMPLE (MiniNN-V1)';

PRINT '============================================================';GOPRINT 'Ingesting a sample neural network model atomically...';

GO

GO

DECLARE @existing_model_id INT;

SELECT @existing_model_id = ModelId FROM dbo.Models WHERE model_name = 'MiniNN-V1';PRINT '============================================================';



IF @existing_model_id IS NOT NULLPRINT 'ATOMIZED MODEL INGESTION SAMPLE (MiniNN-V1)';INSERT INTO dbo.Models (model_name, model_type, architecture, parameter_count)

BEGIN

    PRINT 'Removing existing MiniNN-V1 artifacts...';PRINT '============================================================';VALUES ('MiniNN-V1', 'neural_network', 'feedforward', 27);



    DELETE FROM dbo.AtomsGOGO

    WHERE Metadata IS NOT NULL

      AND JSON_VALUE(CONVERT(NVARCHAR(MAX), Metadata), '$.modelName') = 'MiniNN-V1';



    DELETE FROM dbo.Models WHERE ModelId = @existing_model_id;DECLARE @existing_model_id INT;DECLARE @model_id INT = SCOPE_IDENTITY();

END;

GOSELECT @existing_model_id = ModelId FROM dbo.Models WHERE model_name = 'MiniNN-V1';



DECLARE @model_id INT;INSERT INTO dbo.ModelLayers (model_id, layer_idx, layer_name, layer_type, parameter_count, parameters)



INSERT INTO dbo.Models (model_name, model_type, architecture, parameter_count, config)IF @existing_model_id IS NOT NULLVALUES

VALUES (

    'MiniNN-V1',BEGIN    (@model_id, 0, 'input_layer', 'input', 3, JSON_OBJECT('units': 3, 'activation': 'none'));

    'neural_network',

    'feedforward',    PRINT 'Removing existing MiniNN-V1 artifacts...';

    27,

    JSON_OBJECT(INSERT INTO dbo.ModelLayers (model_id, layer_idx, layer_name, layer_type, parameter_count, parameters)

        'description': 'Sample 3-layer feedforward network',

        'layers': 3,    DELETE FROM dbo.AtomsVALUES

        'embeddingDimension': 3

    )    WHERE Metadata IS NOT NULL    (@model_id, 1, 'hidden_layer', 'dense', 12, JSON_OBJECT('units': 3, 'activation': 'relu'));

);

      AND JSON_VALUE(Metadata, '$.modelName') = 'MiniNN-V1';

SET @model_id = SCOPE_IDENTITY();

CREATE TABLE dbo.NeuronWeights (

PRINT CONCAT('Created model MiniNN-V1 with ModelId=', @model_id);

GO    DELETE FROM dbo.Models WHERE ModelId = @existing_model_id;    weight_id BIGINT PRIMARY KEY IDENTITY(1,1),



DECLARE @Layers TABLE (END;    model_id INT NOT NULL,

    LayerIdx INT PRIMARY KEY,

    LayerName NVARCHAR(64),    layer_idx INT NOT NULL,

    LayerType NVARCHAR(32),

    Units INT,DECLARE @model_id INT;    neuron_idx INT NOT NULL,

    Activation NVARCHAR(32),

    ParameterCount INT    weight_vector VECTOR(3),  -- Input weights

);

INSERT INTO dbo.Models (model_name, model_type, architecture, parameter_count, config)    bias FLOAT,

INSERT INTO @Layers (LayerIdx, LayerName, LayerType, Units, Activation, ParameterCount)

VALUESVALUES (    activation_function NVARCHAR(50),

    (0, 'input_layer', 'input', 3, 'none', 3),

    (1, 'hidden_layer', 'dense', 3, 'relu', 12),    'MiniNN-V1',    FOREIGN KEY (model_id) REFERENCES dbo.Models(model_id)

    (2, 'output_layer', 'dense', 3, 'softmax', 12);

    'neural_network',);

DECLARE @LayerMap TABLE (LayerIdx INT PRIMARY KEY, LayerId BIGINT);

    'feedforward',GO

INSERT INTO dbo.ModelLayers (

    model_id,    27,

    layer_idx,

    layer_name,    JSON_OBJECT(DECLARE @model_id INT = (SELECT TOP 1 model_id FROM dbo.Models ORDER BY model_id DESC);

    layer_type,

    weights,        'description': 'Sample 3-layer feedforward network',

    weights_compressed,

    quantization_type,        'layers': 3,INSERT INTO dbo.NeuronWeights (model_id, layer_idx, neuron_idx, weight_vector, bias, activation_function)

    quantization_scale,

    quantization_zero_point,        'embeddingDimension': 3VALUES

    parameters,

    parameter_count,    )    -- Hidden neuron 1: weights for 3 inputs

    cache_hit_rate,

    avg_compute_time_ms);    (@model_id, 1, 0, CAST('[0.5, -0.3, 0.8]' AS VECTOR(3)), 0.1, 'relu'),

)

OUTPUT inserted.layer_idx, inserted.layer_id INTO @LayerMap(LayerIdx, LayerId)    -- Hidden neuron 2

SELECT

    @model_id,SET @model_id = SCOPE_IDENTITY();    (@model_id, 1, 1, CAST('[-0.2, 0.7, 0.4]' AS VECTOR(3)), -0.2, 'relu'),

    l.LayerIdx,

    l.LayerName,    -- Hidden neuron 3

    l.LayerType,

    NULL,PRINT CONCAT('Created model MiniNN-V1 with ModelId=', @model_id);    (@model_id, 1, 2, CAST('[0.9, 0.1, -0.5]' AS VECTOR(3)), 0.3, 'relu');

    NULL,

    NULL,

    NULL,

    NULL,DECLARE @Layers TABLE (INSERT INTO dbo.ModelLayers (model_id, layer_idx, layer_name, layer_type, parameter_count, parameters)

    JSON_OBJECT('units': l.Units, 'activation': l.Activation),

    l.ParameterCount,    LayerIdx INT PRIMARY KEY,VALUES

    0.0,

    NULL    LayerName NVARCHAR(64),    (@model_id, 2, 'output_layer', 'dense', 12, JSON_OBJECT('units': 3, 'activation': 'softmax'));

FROM @Layers AS l

ORDER BY l.LayerIdx;    LayerType NVARCHAR(32),



PRINT 'Inserted model layers aligned with Atom substrate.';    Units INT,INSERT INTO dbo.NeuronWeights (model_id, layer_idx, neuron_idx, weight_vector, bias, activation_function)

GO

    Activation NVARCHAR(32),VALUES

DECLARE @NeuronWeights TABLE (

    LayerIdx INT,    ParameterCount INT    -- Output neuron 1

    NeuronIdx INT,

    WeightsJson NVARCHAR(200),);    (@model_id, 2, 0, CAST('[0.6, -0.4, 0.2]' AS VECTOR(3)), 0.0, 'softmax'),

    Bias FLOAT,

    Activation NVARCHAR(32)    -- Output neuron 2

);

INSERT INTO @Layers (LayerIdx, LayerName, LayerType, Units, Activation, ParameterCount)    (@model_id, 2, 1, CAST('[-0.3, 0.8, 0.5]' AS VECTOR(3)), 0.1, 'softmax'),

INSERT INTO @NeuronWeights (LayerIdx, NeuronIdx, WeightsJson, Bias, Activation)

VALUESVALUES    -- Output neuron 3

    (1, 0, '[0.5,-0.3,0.8]', 0.1, 'relu'),

    (1, 1, '[-0.2,0.7,0.4]', -0.2, 'relu'),    (0, 'input_layer', 'input', 3, 'none', 3),    (@model_id, 2, 2, CAST('[0.4, 0.2, -0.7]' AS VECTOR(3)), -0.1, 'softmax');

    (1, 2, '[0.9,0.1,-0.5]', 0.3, 'relu'),

    (2, 0, '[0.6,-0.4,0.2]', 0.0, 'softmax'),    (1, 'hidden_layer', 'dense', 3, 'relu', 12),GO

    (2, 1, '[-0.3,0.8,0.5]', 0.1, 'softmax'),

    (2, 2, '[0.4,0.2,-0.7]', -0.1, 'softmax');    (2, 'output_layer', 'dense', 3, 'softmax', 12);



DECLAREPRINT 'Model ingested successfully - weights stored as queryable vectors!';

    @layer_idx INT,

    @neuron_idx INT,DECLARE @LayerMap TABLE (LayerIdx INT PRIMARY KEY, LayerId BIGINT);GO

    @weights_json NVARCHAR(200),

    @bias FLOAT,

    @activation NVARCHAR(32),

    @layer_id BIGINT,INSERT INTO dbo.ModelLayers (CREATE OR ALTER PROCEDURE dbo.sp_InferWithDecomposedModel

    @hash BINARY(32),

    @atom_id BIGINT,    model_id,    @input VECTOR(3),

    @tensor_atom_id BIGINT,

    @w1 FLOAT,    layer_idx,    @model_id INT

    @w2 FLOAT,

    @w3 FLOAT,    layer_name,AS

    @spatial GEOMETRY,

    @spatial_coarse GEOMETRY;    layer_type,BEGIN



DECLARE neuron_cursor CURSOR FOR    weights,    SET NOCOUNT ON;

    SELECT LayerIdx, NeuronIdx, WeightsJson, Bias, Activation

    FROM @NeuronWeights    weights_compressed,

    ORDER BY LayerIdx, NeuronIdx;

    quantization_type,    -- Forward pass through the network using VECTOR operations

OPEN neuron_cursor;

FETCH NEXT FROM neuron_cursor INTO @layer_idx, @neuron_idx, @weights_json, @bias, @activation;    quantization_scale,



WHILE @@FETCH_STATUS = 0    quantization_zero_point,    -- Layer 1: Hidden layer (3 neurons)

BEGIN

    SELECT @layer_id = LayerId FROM @LayerMap WHERE LayerIdx = @layer_idx;    parameters,    DECLARE @hidden_1 FLOAT, @hidden_2 FLOAT, @hidden_3 FLOAT;



    SET @hash = CONVERT(BINARY(32), HASHBYTES('SHA2_256', CONCAT(@model_id, '|', @layer_idx, '|', @neuron_idx, '|', @weights_json, '|', @bias)));    parameter_count,



    INSERT INTO dbo.Atoms (    cache_hit_rate,    -- Neuron 1: dot product + bias, then ReLU

        ContentHash,

        Modality,    avg_compute_time_ms    SELECT @hidden_1 = GREATEST(0,

        Subtype,

        SourceUri,)        (SELECT SUM(

        SourceType,

        CanonicalText,OUTPUT inserted.layer_idx, inserted.layer_id INTO @LayerMap(LayerIdx, LayerId)            CASE idx

        Metadata,

        ReferenceCountSELECT                WHEN 0 THEN CAST(JSON_VALUE(JSON_ARRAY(SELECT value FROM STRING_SPLIT(REPLACE(REPLACE(CAST(@input AS NVARCHAR(100)), '[', ''), ']', ''), ',')), '$[0]') AS FLOAT) * CAST(JSON_VALUE(JSON_ARRAY(SELECT value FROM STRING_SPLIT(REPLACE(REPLACE(CAST(weight_vector AS NVARCHAR(100)), '[', ''), ']', ''), ',')), '$[0]') AS FLOAT)

    )

    VALUES (    @model_id,                WHEN 1 THEN CAST(JSON_VALUE(JSON_ARRAY(SELECT value FROM STRING_SPLIT(REPLACE(REPLACE(CAST(@input AS NVARCHAR(100)), '[', ''), ']', ''), ',')), '$[1]') AS FLOAT) * CAST(JSON_VALUE(JSON_ARRAY(SELECT value FROM STRING_SPLIT(REPLACE(REPLACE(CAST(weight_vector AS NVARCHAR(100)), '[', ''), ']', ''), ',')), '$[1]') AS FLOAT)

        @hash,

        'tensor',    l.LayerIdx,                WHEN 2 THEN CAST(JSON_VALUE(JSON_ARRAY(SELECT value FROM STRING_SPLIT(REPLACE(REPLACE(CAST(@input AS NVARCHAR(100)), '[', ''), ']', ''), ',')), '$[2]') AS FLOAT) * CAST(JSON_VALUE(JSON_ARRAY(SELECT value FROM STRING_SPLIT(REPLACE(REPLACE(CAST(weight_vector AS NVARCHAR(100)), '[', ''), ']', ''), ',')), '$[2]') AS FLOAT)

        'neuron_weight',

        NULL,    l.LayerName,            END

        'model_ingestion',

        CONCAT('MiniNN neuron ', @layer_idx, ':', @neuron_idx, ' (', @activation, ')'),    l.LayerType,        ) + bias

        JSON_OBJECT(

            'modelName': 'MiniNN-V1',    NULL,        FROM dbo.NeuronWeights, (VALUES (0), (1), (2)) AS Numbers(idx)

            'layerIdx': @layer_idx,

            'neuronIdx': @neuron_idx,    NULL,        WHERE model_id = @model_id AND layer_idx = 1 AND neuron_idx = 0

            'activation': @activation,

            'bias': @bias    NULL,    ));

        ),

        1    NULL,

    );

    NULL,    -- Simplified output for demonstration

    SET @atom_id = SCOPE_IDENTITY();

    JSON_OBJECT('units': l.Units, 'activation': l.Activation),    SELECT

    SET @w1 = TRY_CONVERT(FLOAT, JSON_VALUE(@weights_json, '$[0]'));

    SET @w2 = TRY_CONVERT(FLOAT, JSON_VALUE(@weights_json, '$[1]'));    l.ParameterCount,        model_id,

    SET @w3 = TRY_CONVERT(FLOAT, JSON_VALUE(@weights_json, '$[2]'));

    0.0,        layer_idx,

    SET @spatial = geometry::STGeomFromText(CONCAT('POINT(', COALESCE(@w1, 0), ' ', COALESCE(@w2, 0), ' ', COALESCE(@w3, 0), ')'), 0);

    SET @spatial_coarse = geometry::STGeomFromText(CONCAT('POINT(', ROUND(COALESCE(@w1, 0), 1), ' ', ROUND(COALESCE(@w2, 0), 1), ' ', ROUND(COALESCE(@w3, 0), 1), ')'), 0);    NULL        neuron_idx,



    INSERT INTO dbo.AtomEmbeddings (FROM @Layers AS l        weight_vector,

        AtomId,

        ModelId,ORDER BY l.LayerIdx;        bias,

        EmbeddingType,

        Dimension,        activation_function

        EmbeddingVector,

        UsesMaxDimensionPadding,PRINT 'Inserted model layers aligned with Atom substrate.';    FROM dbo.NeuronWeights

        SpatialGeometry,

        SpatialCoarse,    WHERE model_id = @model_id

        Metadata

    )DECLARE @NeuronWeights TABLE (    ORDER BY layer_idx, neuron_idx;

    VALUES (

        @atom_id,    LayerIdx INT,

        @model_id,

        'weight_vector',    NeuronIdx INT,    PRINT 'Inference complete using decomposed model weights!';

        3,

        CAST(@weights_json AS VECTOR(3)),    WeightsJson NVARCHAR(200),    PRINT 'Hidden layer activated: ' + CAST(@hidden_1 AS NVARCHAR(20));

        0,

        @spatial,    Bias FLOAT,END;

        @spatial_coarse,

        JSON_OBJECT('weights': JSON_QUERY(@weights_json))    Activation NVARCHAR(32)GO

    );

);

    INSERT INTO dbo.TensorAtoms (

        AtomId,PRINT 'Testing inference with atomically stored model...';

        ModelId,

        LayerId,INSERT INTO @NeuronWeights (LayerIdx, NeuronIdx, WeightsJson, Bias, Activation)GO

        AtomType,

        SpatialSignature,VALUES

        GeometryFootprint,

        Metadata,    (1, 0, '[0.5,-0.3,0.8]', 0.1, 'relu'),DECLARE @test_input VECTOR(3) = CAST('[1.0, 0.5, -0.3]' AS VECTOR(3));

        ImportanceScore

    )    (1, 1, '[-0.2,0.7,0.4]', -0.2, 'relu'),DECLARE @model_id INT = (SELECT TOP 1 model_id FROM dbo.Models WHERE model_name = 'MiniNN-V1');

    VALUES (

        @atom_id,    (1, 2, '[0.9,0.1,-0.5]', 0.3, 'relu'),

        @model_id,

        @layer_id,    (2, 0, '[0.6,-0.4,0.2]', 0.0, 'softmax'),EXEC dbo.sp_InferWithDecomposedModel @input = @test_input, @model_id = @model_id;

        'neuron_weight',

        @spatial,    (2, 1, '[-0.3,0.8,0.5]', 0.1, 'softmax'),GO

        @spatial_coarse,

        JSON_OBJECT(    (2, 2, '[0.4,0.2,-0.7]', -0.1, 'softmax');

            'modelName': 'MiniNN-V1',

            'layerIdx': @layer_idx,PRINT '';

            'neuronIdx': @neuron_idx

        ),DECLAREPRINT 'Querying model structure atomically:';

        1.0

    );    @layer_idx INT,GO



    SET @tensor_atom_id = SCOPE_IDENTITY();    @neuron_idx INT,



    INSERT INTO dbo.TensorAtomCoefficients (TensorAtomId, ParentLayerId, TensorRole, Coefficient)    @weights_json NVARCHAR(200),SELECT

    VALUES (@tensor_atom_id, @layer_id, 'bias', @bias);

    @bias FLOAT,    m.model_name,

    FETCH NEXT FROM neuron_cursor INTO @layer_idx, @neuron_idx, @weights_json, @bias, @activation;

END    @activation NVARCHAR(32),    ml.layer_idx,



CLOSE neuron_cursor;    @layer_id BIGINT,    ml.layer_name,

DEALLOCATE neuron_cursor;

    @hash BINARY(32),    ml.layer_type,

PRINT 'Stored neuron weights as atoms, embeddings, and tensor atoms.';

GO    @atom_id BIGINT,    ml.parameter_count,



SELECT    @tensor_atom_id BIGINT,    COUNT(nw.weight_id) as neuron_count,

    m.model_name,

    lm.layer_idx,    @w1 FLOAT,    STRING_AGG(CAST(nw.weight_vector AS NVARCHAR(100)), '; ') as all_weights

    JSON_VALUE(CONVERT(NVARCHAR(MAX), a.Metadata), '$.neuronIdx') AS neuron_idx,

    JSON_VALUE(CONVERT(NVARCHAR(MAX), a.Metadata), '$.activation') AS activation,    @w2 FLOAT,FROM dbo.Models m

    CAST(ae.EmbeddingVector AS NVARCHAR(200)) AS weight_vector,

    tac.Coefficient AS bias    @w3 FLOAT,JOIN dbo.ModelLayers ml ON m.model_id = ml.model_id

FROM dbo.Models AS m

INNER JOIN dbo.ModelLayers AS lm ON lm.model_id = m.model_id    @spatial GEOMETRY,LEFT JOIN dbo.NeuronWeights nw ON m.model_id = nw.model_id AND ml.layer_idx = nw.layer_idx

LEFT JOIN dbo.TensorAtoms AS ta ON ta.ModelId = m.model_id AND ta.LayerId = lm.layer_id

LEFT JOIN dbo.Atoms AS a ON a.AtomId = ta.AtomId    @spatial_coarse GEOMETRY;WHERE m.model_name = 'MiniNN-V1'

LEFT JOIN dbo.AtomEmbeddings AS ae ON ae.AtomId = a.AtomId

LEFT JOIN dbo.TensorAtomCoefficients AS tac ON tac.TensorAtomId = ta.TensorAtomId AND tac.TensorRole = 'bias'GROUP BY m.model_name, ml.layer_idx, ml.layer_name, ml.layer_type, ml.parameter_count

WHERE m.model_name = 'MiniNN-V1'

ORDER BY lm.layer_idx, neuron_idx;DECLARE neuron_cursor CURSOR FORORDER BY ml.layer_idx;

GO

    SELECT LayerIdx, NeuronIdx, WeightsJson, Bias, ActivationGO

PRINT 'Atomized model ingestion complete.';

GO    FROM @NeuronWeights



-- =============================================    ORDER BY LayerIdx, NeuronIdx;PRINT '';

-- Atomized Inference Procedure

-- =============================================PRINT '=== Model successfully decomposed into atomic SQL primitives ===';

CREATE OR ALTER PROCEDURE dbo.sp_InferWithAtomizedModel

    @input VECTOR(3),OPEN neuron_cursor;PRINT 'Weights are VECTORS, queryable with VECTOR_DISTANCE';

    @model_id INT

ASFETCH NEXT FROM neuron_cursor INTO @layer_idx, @neuron_idx, @weights_json, @bias, @activation;PRINT 'Neurons are rows, layers are tables';

BEGIN

    SET NOCOUNT ON;PRINT 'Inference = SQL queries, not GPU operations!';



    IF @input IS NULLWHILE @@FETCH_STATUS = 0GO

    BEGIN

        RAISERROR('Input vector is required.', 16, 1);BEGIN

        RETURN;    SELECT @layer_id = LayerId FROM @LayerMap WHERE LayerIdx = @layer_idx;

    END;

    SET @hash = CONVERT(BINARY(32), HASHBYTES('SHA2_256', CONCAT(@model_id, '|', @layer_idx, '|', @neuron_idx, '|', @weights_json, '|', @bias)));

    DECLARE @input_json NVARCHAR(MAX) = CONVERT(NVARCHAR(MAX), @input);

    INSERT INTO dbo.Atoms (

    WITH InputValues AS (        ContentHash,

        SELECT CAST([key] AS INT) AS Position, CAST(value AS FLOAT) AS Value        Modality,

        FROM OPENJSON(@input_json)        Subtype,

    ),        SourceUri,

    Neurons AS (        SourceType,

        SELECT        CanonicalText,

            CAST(JSON_VALUE(CONVERT(NVARCHAR(MAX), a.Metadata), '$.layerIdx') AS INT) AS LayerIdx,        Metadata,

            CAST(JSON_VALUE(CONVERT(NVARCHAR(MAX), a.Metadata), '$.neuronIdx') AS INT) AS NeuronIdx,        ReferenceCount

            JSON_VALUE(CONVERT(NVARCHAR(MAX), a.Metadata), '$.activation') AS Activation,    )

            TRY_CAST(JSON_VALUE(CONVERT(NVARCHAR(MAX), a.Metadata), '$.bias') AS FLOAT) AS Bias,    VALUES (

            JSON_QUERY(CONVERT(NVARCHAR(MAX), ae.Metadata), '$.weights') AS WeightsJson        @hash,

        FROM dbo.TensorAtoms ta        'tensor',

        INNER JOIN dbo.Atoms a ON a.AtomId = ta.AtomId        'neuron_weight',

        INNER JOIN dbo.AtomEmbeddings ae ON ae.AtomId = a.AtomId        NULL,

        WHERE ta.ModelId = @model_id        'model_ingestion',

          AND ta.AtomType = 'neuron_weight'        CONCAT('MiniNN neuron ', @layer_idx, ':', @neuron_idx, ' (', @activation, ')'),

    ),        JSON_OBJECT(

    HiddenLayer AS (            'modelName': 'MiniNN-V1',

        SELECT * FROM Neurons WHERE LayerIdx = 1            'layerIdx': @layer_idx,

    ),            'neuronIdx': @neuron_idx,

    HiddenSums AS (            'activation': @activation,

        SELECT            'bias': @bias

            h.NeuronIdx,        ),

            h.Activation,        1

            SUM(CAST(w.value AS FLOAT) * iv.Value) + h.Bias AS WeightedSum    );

        FROM HiddenLayer h

        CROSS APPLY OPENJSON(h.WeightsJson) w    SET @atom_id = SCOPE_IDENTITY();

        INNER JOIN InputValues iv ON iv.Position = CAST(w.[key] AS INT)

        GROUP BY h.NeuronIdx, h.Activation, h.Bias    SET @w1 = TRY_CONVERT(FLOAT, JSON_VALUE(@weights_json, '$[0]'));

    ),    SET @w2 = TRY_CONVERT(FLOAT, JSON_VALUE(@weights_json, '$[1]'));

    HiddenActivations AS (    SET @w3 = TRY_CONVERT(FLOAT, JSON_VALUE(@weights_json, '$[2]'));

        SELECT

            NeuronIdx,    SET @spatial = geometry::STGeomFromText(CONCAT('POINT(', COALESCE(@w1, 0), ' ', COALESCE(@w2, 0), ' ', COALESCE(@w3, 0), ')'), 0);

            Activation,    SET @spatial_coarse = geometry::STGeomFromText(CONCAT('POINT(', ROUND(COALESCE(@w1, 0), 1), ' ', ROUND(COALESCE(@w2, 0), 1), ' ', ROUND(COALESCE(@w3, 0), 1), ')'), 0);

            WeightedSum AS PreActivation,

            CASE    INSERT INTO dbo.AtomEmbeddings (

                WHEN Activation = 'relu' THEN CASE WHEN WeightedSum > 0 THEN WeightedSum ELSE 0 END        AtomId,

                ELSE WeightedSum        ModelId,

            END AS ActivationValue        EmbeddingType,

        FROM HiddenSums        Dimension,

    ),        EmbeddingVector,

    OutputLayer AS (        UsesMaxDimensionPadding,

        SELECT * FROM Neurons WHERE LayerIdx = 2        SpatialGeometry,

    ),        SpatialCoarse,

    OutputSums AS (        Metadata

        SELECT    )

            o.NeuronIdx,    VALUES (

            o.Activation,        @atom_id,

            o.Bias,        @model_id,

            SUM(CAST(w.value AS FLOAT) * ha.ActivationValue) AS WeightedSum        'weight_vector',

        FROM OutputLayer o        3,

        CROSS APPLY OPENJSON(o.WeightsJson) w        CAST(@weights_json AS VECTOR(3)),

        INNER JOIN HiddenActivations ha ON ha.NeuronIdx = CAST(w.[key] AS INT)        0,

        GROUP BY o.NeuronIdx, o.Activation, o.Bias        @spatial,

    ),        @spatial_coarse,

    OutputActivations AS (        JSON_OBJECT('weights': JSON_QUERY(@weights_json))

        SELECT    );

            NeuronIdx,

            Activation,    INSERT INTO dbo.TensorAtoms (

            WeightedSum + Bias AS PreActivation,        AtomId,

            WeightedSum + Bias AS RawValue        ModelId,

        FROM OutputSums        LayerId,

    )        AtomType,

    SELECT        SpatialSignature,

        'hidden' AS LayerStage,        GeometryFootprint,

        ha.NeuronIdx,        Metadata,

        ha.Activation,        ImportanceScore

        ha.PreActivation,    )

        ha.ActivationValue    VALUES (

    FROM HiddenActivations ha        @atom_id,

        @model_id,

    UNION ALL        @layer_id,

        'neuron_weight',

    SELECT        @spatial,

        'output' AS LayerStage,        @spatial_coarse,

        oa.NeuronIdx,        JSON_OBJECT(

        oa.Activation,            'modelName': 'MiniNN-V1',

        oa.PreActivation,            'layerIdx': @layer_idx,

        CASE            'neuronIdx': @neuron_idx

            WHEN oa.Activation = 'softmax' THEN EXP(oa.RawValue) / NULLIF(SUM(EXP(oa.RawValue)) OVER (), 0)        ),

            ELSE oa.RawValue        1.0

        END AS ActivationValue    );

    FROM OutputActivations oa

    ORDER BY LayerStage, NeuronIdx;    SET @tensor_atom_id = SCOPE_IDENTITY();

END;

GO    INSERT INTO dbo.TensorAtomCoefficients (TensorAtomId, ParentLayerId, TensorRole, Coefficient)

    VALUES (@tensor_atom_id, @layer_id, 'bias', @bias);

PRINT 'Testing atomized inference procedure...';

GO    FETCH NEXT FROM neuron_cursor INTO @layer_idx, @neuron_idx, @weights_json, @bias, @activation;

END

DECLARE @mini_model_id INT = (SELECT ModelId FROM dbo.Models WHERE model_name = 'MiniNN-V1');

DECLARE @test_vector VECTOR(3) = CAST('[1.0,0.5,-0.3]' AS VECTOR(3));CLOSE neuron_cursor;

DEALLOCATE neuron_cursor;

EXEC dbo.sp_InferWithAtomizedModel @input = @test_vector, @model_id = @mini_model_id;

GOPRINT 'Stored neuron weights as atoms, embeddings, and tensor atoms.';


SELECT
    m.model_name,
    lm.layer_idx,
    JSON_VALUE(CONVERT(NVARCHAR(MAX), a.Metadata), '$.neuronIdx') AS neuron_idx,
    JSON_VALUE(CONVERT(NVARCHAR(MAX), a.Metadata), '$.activation') AS activation,
    CAST(ae.EmbeddingVector AS NVARCHAR(200)) AS weight_vector,
    tac.Coefficient AS bias
FROM dbo.Models AS m
INNER JOIN dbo.ModelLayers AS lm ON lm.model_id = m.model_id
LEFT JOIN dbo.TensorAtoms AS ta ON ta.ModelId = m.model_id AND ta.LayerId = lm.layer_id
LEFT JOIN dbo.Atoms AS a ON a.AtomId = ta.AtomId
LEFT JOIN dbo.AtomEmbeddings AS ae ON ae.AtomId = a.AtomId
LEFT JOIN dbo.TensorAtomCoefficients AS tac ON tac.TensorAtomId = ta.TensorAtomId AND tac.TensorRole = 'bias'
WHERE m.model_name = 'MiniNN-V1'
ORDER BY lm.layer_idx, neuron_idx;

PRINT 'Atomized model ingestion complete.';
GO

CREATE OR ALTER PROCEDURE dbo.sp_InferWithAtomizedModel
    @input VECTOR(1998),
    @model_id INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @input_json NVARCHAR(MAX) = CAST(@input AS NVARCHAR(MAX));

    IF @input_json IS NULL
    BEGIN
        RAISERROR('Input vector is required.', 16, 1);
        RETURN;
    END;

    WITH InputValues AS (
        SELECT CAST([key] AS INT) AS Position, CAST(value AS FLOAT) AS Value
        FROM OPENJSON(@input_json)
    ),
    Neurons AS (
        SELECT
            CAST(JSON_VALUE(CONVERT(NVARCHAR(MAX), a.Metadata), '$.layerIdx') AS INT) AS LayerIdx,
            CAST(JSON_VALUE(CONVERT(NVARCHAR(MAX), a.Metadata), '$.neuronIdx') AS INT) AS NeuronIdx,
            JSON_VALUE(CONVERT(NVARCHAR(MAX), a.Metadata), '$.activation') AS Activation,
            TRY_CAST(JSON_VALUE(CONVERT(NVARCHAR(MAX), a.Metadata), '$.bias') AS FLOAT) AS Bias,
            JSON_QUERY(CONVERT(NVARCHAR(MAX), ae.Metadata), '$.weights') AS WeightsJson
        FROM dbo.TensorAtoms ta
        INNER JOIN dbo.Atoms a ON a.AtomId = ta.AtomId
        INNER JOIN dbo.AtomEmbeddings ae ON ae.AtomId = a.AtomId
        WHERE ta.ModelId = @model_id
          AND ta.AtomType = 'neuron_weight'
    ),
    HiddenLayer AS (
        SELECT * FROM Neurons WHERE LayerIdx = 1
    ),
    HiddenSums AS (
        SELECT
            h.NeuronIdx,
            h.Activation,
            h.Bias,
            SUM(CAST(w.value AS FLOAT) * iv.Value) AS WeightedSum
        FROM HiddenLayer h
        CROSS APPLY OPENJSON(h.WeightsJson) w
        INNER JOIN InputValues iv ON iv.Position = CAST(w.[key] AS INT)
        GROUP BY h.NeuronIdx, h.Activation, h.Bias
    ),
    HiddenActivations AS (
        SELECT
            NeuronIdx,
            Activation,
            Bias,
            WeightedSum + Bias AS PreActivation,
            CASE
                WHEN Activation = 'relu' THEN CASE WHEN WeightedSum + Bias > 0 THEN WeightedSum + Bias ELSE 0 END
                ELSE WeightedSum + Bias
            END AS ActivationValue
        FROM HiddenSums
    ),
    OutputLayer AS (
        SELECT * FROM Neurons WHERE LayerIdx = 2
    ),
    OutputSums AS (
        SELECT
            o.NeuronIdx,
            o.Activation,
            o.Bias,
            SUM(CAST(w.value AS FLOAT) * ha.ActivationValue) AS WeightedSum
        FROM OutputLayer o
        CROSS APPLY OPENJSON(o.WeightsJson) w
        INNER JOIN HiddenActivations ha ON ha.NeuronIdx = CAST(w.[key] AS INT)
        GROUP BY o.NeuronIdx, o.Activation, o.Bias
    ),
    OutputActivations AS (
        SELECT
            NeuronIdx,
            Activation,
            Bias,
            WeightedSum + Bias AS PreActivation,
            WeightedSum + Bias AS RawValue
        FROM OutputSums
    )
    SELECT
        'hidden' AS LayerStage,
        ha.NeuronIdx,
        ha.Activation,
        ha.PreActivation,
        ha.ActivationValue
    FROM HiddenActivations ha

    UNION ALL

    SELECT
        'output' AS LayerStage,
        oa.NeuronIdx,
        oa.Activation,
        oa.PreActivation,
        CASE
            WHEN oa.Activation = 'softmax' THEN EXP(oa.RawValue) / NULLIF(SUM(EXP(oa.RawValue)) OVER (), 0)
            ELSE oa.RawValue
        END AS ActivationValue
    FROM OutputActivations oa
    ORDER BY LayerStage, NeuronIdx;
END;
GO

PRINT 'Testing atomized inference procedure...';
GO

DECLARE @mini_model_id INT = (SELECT ModelId FROM dbo.Models WHERE model_name = 'MiniNN-V1');
DECLARE @test_vector VECTOR(3) = CAST('[1.0,0.5,-0.3]' AS VECTOR(3));

EXEC dbo.sp_InferWithAtomizedModel @input = @test_vector, @model_id = @mini_model_id;
GO

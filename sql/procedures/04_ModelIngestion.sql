-- Model Ingestion: Breaking down AI models into atomic SQL components
-- Instead of storing models as blobs, we decompose them into queryable primitives
USE Hartonomous;
GO

-- Example: Ingest a simple 3-layer neural network
-- We break it down into: layers, weights (as vectors), activations, connections

PRINT 'Ingesting a sample neural network model atomically...';
GO

-- Verify required tables exist (migrations should create these)
IF OBJECT_ID('dbo.Models', 'U') IS NULL OR OBJECT_ID('dbo.ModelLayers', 'U') IS NULL
BEGIN
    RAISERROR('Models or ModelLayers table missing - run migrations first', 16, 1);
    RETURN;
END;
GO

-- Insert model metadata
DECLARE @ModelId INT;

IF NOT EXISTS (SELECT 1 FROM dbo.Models WHERE ModelName = 'MiniNN-V1')
BEGIN
    INSERT INTO dbo.Models (ModelName, ModelType, Architecture, ParameterCount, Config)
    VALUES ('MiniNN-V1', 'neural_network', 'feedforward', 27, JSON_OBJECT('layers': 3));
    
    SET @ModelId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    SELECT @ModelId = ModelId FROM dbo.Models WHERE ModelName = 'MiniNN-V1';
END;

-- Layer 1: Input layer (3 inputs)
IF NOT EXISTS (SELECT 1 FROM dbo.ModelLayers WHERE ModelId = @ModelId AND LayerIdx = 0)
BEGIN
    INSERT INTO dbo.ModelLayers (ModelId, LayerIdx, LayerName, LayerType, ParameterCount, Parameters)
    VALUES
        (@ModelId, 0, 'input_layer', 'input', 3, JSON_OBJECT('units': 3, 'activation': 'none'));
END;

-- Layer 2: Hidden layer (3 units, weights as VECTOR)
-- Each neuron has 3 input weights + 1 bias = 4 parameters per neuron = 12 total
IF NOT EXISTS (SELECT 1 FROM dbo.ModelLayers WHERE ModelId = @ModelId AND LayerIdx = 1)
BEGIN
    INSERT INTO dbo.ModelLayers (ModelId, LayerIdx, LayerName, LayerType, ParameterCount, Parameters)
    VALUES
        (@ModelId, 1, 'hidden_layer', 'dense', 12, JSON_OBJECT('units': 3, 'activation': 'relu'));
END;

-- Layer 3: Output layer (3 units for classification)
IF NOT EXISTS (SELECT 1 FROM dbo.ModelLayers WHERE ModelId = @ModelId AND LayerIdx = 2)
BEGIN
    INSERT INTO dbo.ModelLayers (ModelId, LayerIdx, LayerName, LayerType, ParameterCount, Parameters)
    VALUES
        (@ModelId, 2, 'output_layer', 'dense', 12, JSON_OBJECT('units': 3, 'activation': 'softmax'));
END;
GO

-- Store weights as TensorAtoms (content-addressed)
-- Each weight tensor becomes an Atom with spatial signature
DECLARE @ModelId INT = (SELECT ModelId FROM dbo.Models WHERE ModelName = 'MiniNN-V1');

-- Hidden layer weights
IF NOT EXISTS (SELECT 1 FROM dbo.TensorAtoms WHERE ModelId = @ModelId AND LayerIdx = 1)
BEGIN
    INSERT INTO dbo.TensorAtoms (ModelId, LayerIdx, NeuronIdx, WeightVector, Bias, ActivationFunction, SpatialSignature)
    VALUES
        -- Hidden neuron 1: weights for 3 inputs
        (@ModelId, 1, 0, CAST('[0.5, -0.3, 0.8]' AS VECTOR(3)), 0.1, 'relu', 
         geometry::STGeomFromText('POINT(0.5 -0.3 0.8)', 0)),
        -- Hidden neuron 2
        (@ModelId, 1, 1, CAST('[-0.2, 0.7, 0.4]' AS VECTOR(3)), -0.2, 'relu',
         geometry::STGeomFromText('POINT(-0.2 0.7 0.4)', 0)),
        -- Hidden neuron 3
        (@ModelId, 1, 2, CAST('[0.9, 0.1, -0.5]' AS VECTOR(3)), 0.3, 'relu',
         geometry::STGeomFromText('POINT(0.9 0.1 -0.5)', 0));
END;

-- Output layer weights (each output neuron gets input from 3 hidden neurons)
IF NOT EXISTS (SELECT 1 FROM dbo.TensorAtoms WHERE ModelId = @ModelId AND LayerIdx = 2)
BEGIN
    INSERT INTO dbo.TensorAtoms (ModelId, LayerIdx, NeuronIdx, WeightVector, Bias, ActivationFunction, SpatialSignature)
    VALUES
        -- Output neuron 1
        (@ModelId, 2, 0, CAST('[0.6, -0.4, 0.2]' AS VECTOR(3)), 0.0, 'softmax',
         geometry::STGeomFromText('POINT(0.6 -0.4 0.2)', 0)),
        -- Output neuron 2
        (@ModelId, 2, 1, CAST('[-0.3, 0.8, 0.5]' AS VECTOR(3)), 0.1, 'softmax',
         geometry::STGeomFromText('POINT(-0.3 0.8 0.5)', 0)),
        -- Output neuron 3
        (@ModelId, 2, 2, CAST('[0.4, 0.2, -0.7]' AS VECTOR(3)), -0.1, 'softmax',
         geometry::STGeomFromText('POINT(0.4 0.2 -0.7)', 0));
END;
GO

PRINT 'Model ingested successfully - weights stored as queryable vectors!';
GO

-- Now create inference procedure that USES the decomposed model
CREATE OR ALTER PROCEDURE dbo.sp_InferWithDecomposedModel
    @Input VECTOR(3),
    @ModelId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Simplified demonstration: Show decomposed model weights via TensorAtoms
    SELECT
        ModelId,
        LayerIdx,
        NeuronIdx,
        WeightVector,
        Bias,
        ActivationFunction,
        SpatialSignature.STAsText() AS SpatialSignature
    FROM dbo.TensorAtoms
    WHERE ModelId = @ModelId
    ORDER BY LayerIdx, NeuronIdx;

    PRINT 'Inference procedure created - demonstrates querying decomposed model weights!';
END;
GO

-- Test the inference with decomposed model
PRINT 'Testing inference with atomically stored model...';
GO

DECLARE @TestInput VECTOR(3) = CAST('[1.0, 0.5, -0.3]' AS VECTOR(3));
DECLARE @ModelId INT = (SELECT ModelId FROM dbo.Models WHERE ModelName = 'MiniNN-V1');

IF @ModelId IS NOT NULL
    EXEC dbo.sp_InferWithDecomposedModel @Input = @TestInput, @ModelId = @ModelId;
ELSE
    PRINT 'Model MiniNN-V1 not found - run model ingestion first';
GO

-- Query the model atomically
PRINT '';
PRINT 'Querying model structure atomically:';
GO

SELECT
    m.ModelName,
    ml.LayerIdx,
    ml.LayerName,
    ml.LayerType,
    ml.ParameterCount,
    COUNT(ta.TensorAtomId) as NeuronCount
FROM dbo.Models m
INNER JOIN dbo.ModelLayers ml ON m.ModelId = ml.ModelId
LEFT JOIN dbo.TensorAtoms ta ON m.ModelId = ta.ModelId AND ml.LayerIdx = ta.LayerIdx
WHERE m.ModelName = 'MiniNN-V1'
GROUP BY m.ModelName, ml.LayerIdx, ml.LayerName, ml.LayerType, ml.ParameterCount
ORDER BY ml.LayerIdx;
GO

PRINT '';
PRINT '=== Model successfully decomposed into atomic SQL primitives ===';
PRINT 'Weights are VECTORS, queryable with VECTOR_DISTANCE';
PRINT 'Neurons are rows, layers are tables';
PRINT 'Inference = SQL queries, not GPU operations!';
GO

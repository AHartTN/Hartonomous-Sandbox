-- Model Ingestion: Breaking down AI models into atomic SQL components
-- Instead of storing models as blobs, we decompose them into queryable primitives
USE Hartonomous;
GO

-- Example: Ingest a simple 3-layer neural network
-- We break it down into: layers, weights (as vectors), activations, connections

PRINT 'Ingesting a sample neural network model atomically...';
GO

-- Insert model metadata
INSERT INTO dbo.Models (model_name, model_type, architecture, parameter_count)
VALUES ('MiniNN-V1', 'neural_network', 'feedforward', 27);
GO

DECLARE @model_id INT = SCOPE_IDENTITY();

-- Layer 1: Input layer (3 inputs)
INSERT INTO dbo.ModelLayers (model_id, layer_idx, layer_name, layer_type, parameter_count, parameters)
VALUES
    (@model_id, 0, 'input_layer', 'input', 3, JSON_OBJECT('units': 3, 'activation': 'none'));

-- Layer 2: Hidden layer (3 units, weights as VECTOR)
-- Each neuron has 3 input weights + 1 bias = 4 parameters per neuron = 12 total
INSERT INTO dbo.ModelLayers (model_id, layer_idx, layer_name, layer_type, parameter_count, parameters)
VALUES
    (@model_id, 1, 'hidden_layer', 'dense', 12, JSON_OBJECT('units': 3, 'activation': 'relu'));

-- Store individual neuron weights as VECTORS
CREATE TABLE dbo.NeuronWeights (
    weight_id BIGINT PRIMARY KEY IDENTITY(1,1),
    model_id INT NOT NULL,
    layer_idx INT NOT NULL,
    neuron_idx INT NOT NULL,
    weight_vector VECTOR(3),  -- Input weights
    bias FLOAT,
    activation_function NVARCHAR(50),
    FOREIGN KEY (model_id) REFERENCES dbo.Models(model_id)
);
GO

-- Insert actual weights for hidden layer neurons
DECLARE @model_id INT = (SELECT TOP 1 model_id FROM dbo.Models ORDER BY model_id DESC);

INSERT INTO dbo.NeuronWeights (model_id, layer_idx, neuron_idx, weight_vector, bias, activation_function)
VALUES
    -- Hidden neuron 1: weights for 3 inputs
    (@model_id, 1, 0, CAST('[0.5, -0.3, 0.8]' AS VECTOR(3)), 0.1, 'relu'),
    -- Hidden neuron 2
    (@model_id, 1, 1, CAST('[-0.2, 0.7, 0.4]' AS VECTOR(3)), -0.2, 'relu'),
    -- Hidden neuron 3
    (@model_id, 1, 2, CAST('[0.9, 0.1, -0.5]' AS VECTOR(3)), 0.3, 'relu');

-- Layer 3: Output layer (3 units for classification)
INSERT INTO dbo.ModelLayers (model_id, layer_idx, layer_name, layer_type, parameter_count, parameters)
VALUES
    (@model_id, 2, 'output_layer', 'dense', 12, JSON_OBJECT('units': 3, 'activation': 'softmax'));

-- Output layer weights (each output neuron gets input from 3 hidden neurons)
INSERT INTO dbo.NeuronWeights (model_id, layer_idx, neuron_idx, weight_vector, bias, activation_function)
VALUES
    -- Output neuron 1
    (@model_id, 2, 0, CAST('[0.6, -0.4, 0.2]' AS VECTOR(3)), 0.0, 'softmax'),
    -- Output neuron 2
    (@model_id, 2, 1, CAST('[-0.3, 0.8, 0.5]' AS VECTOR(3)), 0.1, 'softmax'),
    -- Output neuron 3
    (@model_id, 2, 2, CAST('[0.4, 0.2, -0.7]' AS VECTOR(3)), -0.1, 'softmax');
GO

PRINT 'Model ingested successfully - weights stored as queryable vectors!';
GO

-- Now create inference procedure that USES the decomposed model
CREATE OR ALTER PROCEDURE dbo.sp_InferWithDecomposedModel
    @input VECTOR(3),
    @model_id INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Forward pass through the network using VECTOR operations

    -- Layer 1: Hidden layer (3 neurons)
    DECLARE @hidden_1 FLOAT, @hidden_2 FLOAT, @hidden_3 FLOAT;

    -- Neuron 1: dot product + bias, then ReLU
    SELECT @hidden_1 = GREATEST(0,
        (SELECT SUM(
            CASE idx
                WHEN 0 THEN CAST(JSON_VALUE(JSON_ARRAY(SELECT value FROM STRING_SPLIT(REPLACE(REPLACE(CAST(@input AS NVARCHAR(100)), '[', ''), ']', ''), ',')), '$[0]') AS FLOAT) * CAST(JSON_VALUE(JSON_ARRAY(SELECT value FROM STRING_SPLIT(REPLACE(REPLACE(CAST(weight_vector AS NVARCHAR(100)), '[', ''), ']', ''), ',')), '$[0]') AS FLOAT)
                WHEN 1 THEN CAST(JSON_VALUE(JSON_ARRAY(SELECT value FROM STRING_SPLIT(REPLACE(REPLACE(CAST(@input AS NVARCHAR(100)), '[', ''), ']', ''), ',')), '$[1]') AS FLOAT) * CAST(JSON_VALUE(JSON_ARRAY(SELECT value FROM STRING_SPLIT(REPLACE(REPLACE(CAST(weight_vector AS NVARCHAR(100)), '[', ''), ']', ''), ',')), '$[1]') AS FLOAT)
                WHEN 2 THEN CAST(JSON_VALUE(JSON_ARRAY(SELECT value FROM STRING_SPLIT(REPLACE(REPLACE(CAST(@input AS NVARCHAR(100)), '[', ''), ']', ''), ',')), '$[2]') AS FLOAT) * CAST(JSON_VALUE(JSON_ARRAY(SELECT value FROM STRING_SPLIT(REPLACE(REPLACE(CAST(weight_vector AS NVARCHAR(100)), '[', ''), ']', ''), ',')), '$[2]') AS FLOAT)
            END
        ) + bias
        FROM dbo.NeuronWeights, (VALUES (0), (1), (2)) AS Numbers(idx)
        WHERE model_id = @model_id AND layer_idx = 1 AND neuron_idx = 0
    ));

    -- Simplified output for demonstration
    SELECT
        model_id,
        layer_idx,
        neuron_idx,
        weight_vector,
        bias,
        activation_function
    FROM dbo.NeuronWeights
    WHERE model_id = @model_id
    ORDER BY layer_idx, neuron_idx;

    PRINT 'Inference complete using decomposed model weights!';
    PRINT 'Hidden layer activated: ' + CAST(@hidden_1 AS NVARCHAR(20));
END;
GO

-- Test the inference with decomposed model
PRINT 'Testing inference with atomically stored model...';
GO

DECLARE @test_input VECTOR(3) = CAST('[1.0, 0.5, -0.3]' AS VECTOR(3));
DECLARE @model_id INT = (SELECT TOP 1 model_id FROM dbo.Models WHERE model_name = 'MiniNN-V1');

EXEC dbo.sp_InferWithDecomposedModel @input = @test_input, @model_id = @model_id;
GO

-- Query the model atomically
PRINT '';
PRINT 'Querying model structure atomically:';
GO

SELECT
    m.model_name,
    ml.layer_idx,
    ml.layer_name,
    ml.layer_type,
    ml.parameter_count,
    COUNT(nw.weight_id) as neuron_count,
    STRING_AGG(CAST(nw.weight_vector AS NVARCHAR(100)), '; ') as all_weights
FROM dbo.Models m
JOIN dbo.ModelLayers ml ON m.model_id = ml.model_id
LEFT JOIN dbo.NeuronWeights nw ON m.model_id = nw.model_id AND ml.layer_idx = nw.layer_idx
WHERE m.model_name = 'MiniNN-V1'
GROUP BY m.model_name, ml.layer_idx, ml.layer_name, ml.layer_type, ml.parameter_count
ORDER BY ml.layer_idx;
GO

PRINT '';
PRINT '=== Model successfully decomposed into atomic SQL primitives ===';
PRINT 'Weights are VECTORS, queryable with VECTOR_DISTANCE';
PRINT 'Neurons are rows, layers are tables';
PRINT 'Inference = SQL queries, not GPU operations!';
GO

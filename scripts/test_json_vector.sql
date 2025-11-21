-- Test SQL Server 2025 JSON and VECTOR data types

-- Test 1: Native JSON type
DECLARE @j JSON = '{"test": 123}';
SELECT @j AS TestJson;

-- Test 2: VECTOR type  
DECLARE @v VECTOR(3) = '[1.0, 2.0, 3.0]';
SELECT @v AS TestVector;

-- Test 3: JSON functions (built-in, not in sys.objects)
SELECT JSON_VALUE('{"name":"John"}', '$.name') AS JsonValue;
SELECT JSON_QUERY('{"arr":[1,2,3]}', '$.arr') AS JsonQuery;
SELECT ISJSON('{"valid":true}') AS IsValidJson;

-- Test 4: Vector functions (built-in, not in sys.objects)
SELECT VECTOR_DISTANCE('[1.0, 2.0]', '[3.0, 4.0]', 'euclidean') AS EuclideanDistance;
SELECT VECTORPROPERTY('[1.0, 2.0, 3.0]', 'Dimensions') AS VectorDimensions;

-- Test 5: Check database compatibility level
SELECT name, compatibility_level 
FROM sys.databases 
WHERE database_id = DB_ID();

-- Test 6: Check for AI functions
SELECT 
    CASE WHEN OBJECT_ID('AI_GENERATE_EMBEDDINGS') IS NOT NULL 
        THEN 'AI_GENERATE_EMBEDDINGS available' 
        ELSE 'AI_GENERATE_EMBEDDINGS not available' 
    END AS AI_Embeddings_Status,
    CASE WHEN OBJECT_ID('AI_GENERATE_CHUNKS') IS NOT NULL 
        THEN 'AI_GENERATE_CHUNKS available' 
        ELSE 'AI_GENERATE_CHUNKS not available' 
    END AS AI_Chunks_Status;

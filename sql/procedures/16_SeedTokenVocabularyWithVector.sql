-- =============================================
-- Seed Token Vocabulary (Extended) using Atom substrate
-- =============================================

USE Hartonomous;
GO

-- Ensure a model exists for token vocabulary
DECLARE @ModelId INT;
SELECT @ModelId = ModelId FROM dbo.Models WHERE ModelName = 'TokenVocab-Extended';

IF @ModelId IS NULL
BEGIN
    INSERT INTO dbo.Models (ModelName, ModelType, Architecture, ParameterCount, Config)
    VALUES ('TokenVocab-Extended', 'vocabulary', 'tokenizer', 0, JSON_OBJECT('purpose': 'extended token embeddings', 'dimension': 768));
    SET @ModelId = SCOPE_IDENTITY();
END;

-- Insert tokens as Atoms (content-addressed)
MERGE INTO dbo.Atoms AS target
USING (
    VALUES
        ('hello', HASHBYTES('SHA2_256', CAST('token:hello:ext' AS VARBINARY(MAX))), 'text', CAST('hello' AS VARBINARY(MAX)), 'text/plain'),
        ('world', HASHBYTES('SHA2_256', CAST('token:world:ext' AS VARBINARY(MAX))), 'text', CAST('world' AS VARBINARY(MAX)), 'text/plain'),
        ('[EOS]', HASHBYTES('SHA2_256', CAST('token:[EOS]:ext' AS VARBINARY(MAX))), 'text', CAST('[EOS]' AS VARBINARY(MAX)), 'text/plain')
) AS source(Token, AtomHash, AtomType, AtomData, ContentType)
ON target.AtomHash = source.AtomHash
WHEN NOT MATCHED THEN
    INSERT (AtomHash, AtomType, AtomData, ContentType)
    VALUES (source.AtomHash, source.AtomType, source.AtomData, source.ContentType);

-- Build proper VECTOR arrays (768 dimensions)
DECLARE @vec1 NVARCHAR(MAX) = '1.0,' + REPLICATE('0.0,', 766) + '0.0';
DECLARE @vec2 NVARCHAR(MAX) = '0.5,0.5,' + REPLICATE('0.0,', 765) + '0.0';
DECLARE @vec3 NVARCHAR(MAX) = REPLICATE('0.0,', 767) + '0.0';

-- Insert embeddings linked to Atoms
MERGE INTO dbo.AtomEmbeddings AS target
USING (
    SELECT
        a.AtomId,
        vec.EmbeddingVector,
        768 AS Dimension
    FROM dbo.Atoms a
    CROSS APPLY (
        VALUES
            (CASE CAST(a.AtomData AS NVARCHAR(100))
                WHEN 'hello' THEN @vec1
                WHEN 'world' THEN @vec2
                WHEN '[EOS]' THEN @vec3
            END)
    ) AS vec(EmbeddingVector)
    WHERE a.AtomType = 'text'
      AND CAST(a.AtomData AS NVARCHAR(100)) IN ('hello', 'world', '[EOS]')
      AND a.AtomHash IN (
          HASHBYTES('SHA2_256', CAST('token:hello:ext' AS VARBINARY(MAX))),
          HASHBYTES('SHA2_256', CAST('token:world:ext' AS VARBINARY(MAX))),
          HASHBYTES('SHA2_256', CAST('token:[EOS]:ext' AS VARBINARY(MAX)))
      )
) AS source
ON target.AtomId = source.AtomId
WHEN NOT MATCHED THEN
    INSERT (AtomId, EmbeddingVector, Dimension)
    VALUES (source.AtomId, CAST('[' + source.EmbeddingVector + ']' AS VECTOR(1998)), source.Dimension);

PRINT CONCAT('Seeded 3 extended token atoms for ModelId=', @ModelId);
GO

CREATE PROCEDURE dbo.sp_GenerateTextSpatial
    @prompt NVARCHAR(MAX),
    @max_tokens INT = 10,
    @temperature FLOAT = 1.0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @context TABLE (AtomId BIGINT PRIMARY KEY, AtomText NVARCHAR(100));
    DECLARE @generated_text NVARCHAR(MAX) = @prompt;
    DECLARE @iteration INT = 0;
    DECLARE @context_ids NVARCHAR(MAX);
    DECLARE @NextAtomId BIGINT;
    DECLARE @NextAtomText NVARCHAR(100);

    INSERT INTO @context (AtomId, AtomText)
    SELECT a.AtomId, CAST(a.CanonicalText AS NVARCHAR(100))
    FROM dbo.Atom a
    WHERE CAST(a.CanonicalText AS NVARCHAR(100)) IN (
        SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@prompt, ' ')
    );

    WHILE @iteration < @max_tokens
    BEGIN
        SELECT @context_ids = STRING_AGG(CAST(AtomId AS NVARCHAR(20)), ',')
        FROM @context;

        IF @context_ids IS NULL
        BEGIN
            BREAK;
        END;

        DECLARE @next TABLE (TokenId BIGINT, TokenText NVARCHAR(100), SpatialDistance FLOAT, ProbabilityScore FLOAT);

        INSERT INTO @next
        EXEC dbo.sp_SpatialNextToken
            @context_atom_ids = @context_ids,
            @temperature = @temperature,
            @top_k = 1;

        SELECT TOP 1
            @NextAtomId = TokenId,
            @NextAtomText = TokenText
        FROM @next
        ORDER BY ProbabilityScore DESC;

        IF @NextAtomId IS NULL OR EXISTS (SELECT 1 FROM @context WHERE AtomId = @NextAtomId)
        BEGIN
            BREAK;
        END;

        INSERT INTO @context (AtomId, AtomText) VALUES (@NextAtomId, @NextAtomText);
        SET @generated_text = @generated_text + N' ' + @NextAtomText;
        SET @iteration = @iteration + 1;
    END;

    SELECT
        @prompt AS OriginalPrompt,
        @generated_text AS GeneratedText,
        @iteration AS TokensGenerated,
        'SPATIAL_GEOMETRY_R_TREE' AS Method;
END;
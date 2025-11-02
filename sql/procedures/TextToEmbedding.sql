-- Hartonomous Text-to-Embedding Conversion
-- Uses Azure OpenAI REST API via sp_invoke_external_rest_endpoint
USE Hartonomous;
GO

CREATE OR ALTER PROCEDURE dbo.sp_TextToEmbedding
    @text NVARCHAR(MAX),
    @model_name NVARCHAR(100) = 'text-embedding-3-large',
    @embedding VECTOR(768) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @retval INT;
    DECLARE @response NVARCHAR(MAX);
    DECLARE @payload NVARCHAR(MAX);
    DECLARE @url NVARCHAR(500);

    -- Construct the Azure OpenAI embeddings endpoint URL
    -- Note: Replace 'your-resource-name' with actual Azure OpenAI resource name
    SET @url = 'https://your-resource-name.openai.azure.com/openai/deployments/' +
               @model_name + '/embeddings?api-version=2023-03-15-preview';

    -- Create JSON payload
    SET @payload = JSON_OBJECT('input': @text);

    -- Call Azure OpenAI REST API
    -- Note: Requires database scoped credential for authentication
    EXEC @retval = sp_invoke_external_rest_endpoint
        @url = @url,
        @method = 'POST',
        @payload = @payload,
        @response = @response OUTPUT;

    -- Check for errors
    IF @retval <> 0
    BEGIN
        RAISERROR('Failed to call Azure OpenAI API. Return code: %d', 16, 1, @retval);
        RETURN;
    END

    -- Extract embedding from JSON response
    -- The response format is: {"result": {"data": [{"embedding": [float, float, ...]}]}}
    DECLARE @embedding_json NVARCHAR(MAX);
    SET @embedding_json = JSON_QUERY(@response, '$.result.data[0].embedding');

    IF @embedding_json IS NULL
    BEGIN
        RAISERROR('Invalid response from Azure OpenAI API. Could not extract embedding.', 16, 1);
        RETURN;
    END

    -- Convert JSON array to VECTOR type
    SET @embedding = CAST(@embedding_json AS VECTOR(768));

    -- Log the inference request
    INSERT INTO dbo.InferenceRequests (
        TaskType,
        InputData,
        ModelsUsed,
        EnsembleStrategy,
        OutputMetadata
    )
    VALUES (
        'text_to_embedding',
        @text,
        @model_name,
        'azure_openai_rest',
        JSON_OBJECT(
            'embedding_dimensions': 768,
            'model': @model_name,
            'text_length': LEN(@text)
        )
    );
END;
GO

PRINT 'Text-to-embedding procedure created successfully.';
PRINT 'Note: Update the @url variable with your actual Azure OpenAI resource name.';
GO
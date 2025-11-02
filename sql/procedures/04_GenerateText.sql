-- =============================================
-- Generate Text Stored Procedure
-- =============================================

USE Hartonomous;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GenerateText
	@prompt NVARCHAR(MAX),
	@max_tokens INT = 50,
	@ModelId INT = NULL
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @start_time DATETIME2 = SYSUTCDATETIME();
	DECLARE @generated_text NVARCHAR(MAX) = LTRIM(RTRIM(@prompt));
	DECLARE @current_token NVARCHAR(100) = CASE
		WHEN LEN(LTRIM(RTRIM(@prompt))) = 0 THEN '[BOS]'
		ELSE LOWER(@prompt)
	END;
	DECLARE @next_token NVARCHAR(100);
	DECLARE @iteration INT = 0;
	DECLARE @inference_id BIGINT;
	DECLARE @current_vocab_id INT;
	DECLARE @next_vocab_id INT;

	INSERT INTO dbo.InferenceRequests (TaskType, InputData, ModelsUsed)
	VALUES (
		'text_generation',
		JSON_OBJECT('prompt': @prompt, 'max_tokens': @max_tokens, 'model_id': @ModelId),
		'token_vector_walk'
	);
	SET @inference_id = SCOPE_IDENTITY();

	SELECT TOP 1
		@current_vocab_id = VocabId,
		@current_token = Token
	FROM dbo.TokenVocabulary
	WHERE Token = @current_token
		AND (@ModelId IS NULL OR ModelId = @ModelId)
		AND Embedding IS NOT NULL;

	IF @current_vocab_id IS NULL
	BEGIN
		SELECT TOP 1
			@current_token = Token,
			@current_vocab_id = VocabId
		FROM dbo.TokenVocabulary
		WHERE (@ModelId IS NULL OR ModelId = @ModelId)
			AND Embedding IS NOT NULL
		ORDER BY VocabId;

		IF @current_vocab_id IS NULL
		BEGIN
			RAISERROR('Token vocabulary is empty or lacks embeddings for the specified model.', 16, 1);
			RETURN;
		END

		SET @generated_text = CASE WHEN LEN(@prompt) = 0 THEN @current_token ELSE @generated_text END;
	END

	WHILE @iteration < @max_tokens
	BEGIN
		SELECT TOP 1
			@next_token = tv.Token,
			@next_vocab_id = tv.VocabId
		FROM dbo.TokenVocabulary AS tv
		CROSS JOIN dbo.TokenVocabulary AS currentToken
		WHERE currentToken.VocabId = @current_vocab_id
			AND tv.Embedding IS NOT NULL
			AND currentToken.Embedding IS NOT NULL
			AND (@ModelId IS NULL OR tv.ModelId = @ModelId)
			AND tv.Token <> @current_token
		ORDER BY VECTOR_DISTANCE('cosine', tv.Embedding, currentToken.Embedding);

		IF @next_token IS NULL OR @next_vocab_id IS NULL
		BEGIN
			BREAK;
		END

		SET @generated_text = @generated_text + ' ' + @next_token;
		SET @current_token = @next_token;
		SET @current_vocab_id = @next_vocab_id;
		SET @iteration = @iteration + 1;

		IF @next_token = '[EOS]'
		BEGIN
			BREAK;
		END
	END

	DECLARE @duration INT = DATEDIFF(MILLISECOND, @start_time, SYSUTCDATETIME());

	UPDATE dbo.InferenceRequests
	SET TotalDurationMs = @duration,
		OutputData = JSON_OBJECT('generated_text': @generated_text),
		OutputMetadata = JSON_OBJECT(
			'tokens_generated': @iteration,
			'ended_with_eos': CASE WHEN @next_token = '[EOS]' THEN 1 ELSE 0 END
		)
	WHERE InferenceId = @inference_id;

	INSERT INTO dbo.InferenceSteps (InferenceId, StepNumber, OperationType, DurationMs, QueryText)
	VALUES (
		@inference_id,
		1,
		'token_vector_walk',
		@duration,
		'Iterative nearest-neighbour walk across token embeddings'
	);

	SELECT
		@generated_text AS generated_text,
		@iteration AS tokens_generated,
		CASE WHEN @next_token = '[EOS]' THEN 1 ELSE 0 END AS ended_with_eos;
END
GO
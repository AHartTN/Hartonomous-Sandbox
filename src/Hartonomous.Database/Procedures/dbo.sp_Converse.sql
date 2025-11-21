CREATE PROCEDURE dbo.sp_Converse
    @Prompt NVARCHAR(MAX),
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ToolList NVARCHAR(MAX);
    DECLARE @ToolSelectionPrompt NVARCHAR(MAX);
    DECLARE @ToolSelectionResult NVARCHAR(MAX);
    DECLARE @SelectedToolName NVARCHAR(128);
    DECLARE @SelectedToolParams NVARCHAR(MAX);
    DECLARE @ToolExecutionResult NVARCHAR(MAX);
    DECLARE @SynthesisPrompt NVARCHAR(MAX);
    DECLARE @FinalAnswer NVARCHAR(MAX);

    BEGIN TRY
        -- STEP 1: Get the list of available tools for the LLM to choose from.
        SELECT @ToolList = (
            SELECT ToolName, Description, ParametersJson
            FROM dbo.AgentTools
            WHERE IsEnabled = 1
            FOR JSON PATH
        );

        IF @ToolList IS NULL OR @ToolList = ''
        BEGIN
            RAISERROR('No tools are available for the agent.', 16, 1);
            RETURN;
        END;

        -- STEP 2: Tool Selection (Decide)
        -- Ask the LLM to choose a tool based on the user's prompt.
        SET @ToolSelectionPrompt = N'
You are a helpful AI assistant with access to a set of tools. Your task is to choose the single best tool to answer the user''s prompt.
Respond with a JSON object containing the "tool_name" and the "parameters" to call it with.

USER PROMPT:
"' + @Prompt + N'"

AVAILABLE TOOLS:
' + @ToolList + N'

Based on the user prompt, which tool should you use?

RESPONSE (JSON only):
';
        IF @Debug = 1 PRINT 'Tool Selection Prompt: ' + @ToolSelectionPrompt;

        -- Call the text generation model to get the tool selection
        DECLARE @GenerationResult TABLE (GeneratedText NVARCHAR(MAX));
        INSERT INTO @GenerationResult
        EXEC dbo.sp_GenerateText @prompt = @ToolSelectionPrompt, @temperature = 0.0;

        SELECT TOP 1 @ToolSelectionResult = GeneratedText FROM @GenerationResult;

        IF @Debug = 1 PRINT 'Tool Selection Result: ' + @ToolSelectionResult;

        -- Parse the LLM's decision
        SELECT
            @SelectedToolName = JSON_VALUE(@ToolSelectionResult, '$.tool_name'),
            @SelectedToolParams = JSON_QUERY(@ToolSelectionResult, '$.parameters');

        IF @SelectedToolName IS NULL
        BEGIN
            -- If the model didn't pick a tool, maybe it can answer directly.
            -- For now, we'll treat this as an error.
            RAISERROR('The model did not select a tool to execute.', 16, 1);
            RETURN;
        END;

        -- STEP 3: Tool Execution (Act)
        -- This is a simplified dispatcher. A real implementation would be more robust.
        IF @SelectedToolName = 'analyze_system_state'
        BEGIN
            -- The tool returns a table-valued function, query it directly
            SELECT @ToolExecutionResult = (
                SELECT AnalysisJson 
                FROM dbo.fn_clr_AnalyzeSystemState(NULL) 
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            );
        END
        -- ELSE IF @SelectedToolName = 'another_tool' ...
        ELSE
        BEGIN
            RAISERROR('Selected tool "%s" is not implemented in the dispatcher.', 16, 1, @SelectedToolName);
            RETURN;
        END;

        IF @Debug = 1 PRINT 'Tool Execution Result: ' + @ToolExecutionResult;

        -- STEP 4: Response Synthesis
        -- Ask the LLM to generate a human-readable answer based on the tool's output.
        SET @SynthesisPrompt = N'
You are a helpful AI assistant.
A user asked the following question: "' + @Prompt + N'"
To answer this, you ran the "' + @SelectedToolName + N'" tool and got the following data:
' + @ToolExecutionResult + N'

Based on this data, please provide a clear, concise, and helpful answer to the user''s original question.
Do not just repeat the data; explain what it means.
';
        IF @Debug = 1 PRINT 'Synthesis Prompt: ' + @SynthesisPrompt;

        DELETE FROM @GenerationResult;
        INSERT INTO @GenerationResult
        EXEC dbo.sp_GenerateText @prompt = @SynthesisPrompt, @temperature = 0.7;

        SELECT TOP 1 @FinalAnswer = GeneratedText FROM @GenerationResult;

        -- Return the final answer
        SELECT @FinalAnswer AS FinalAnswer;

    END TRY
    BEGIN CATCH
        -- Error handling
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR('An error occurred in the agent conversation: %s', 16, 1, @ErrorMessage);
    END CATCH;
END;
-- =============================================
-- Agent Framework Foundation
-- =============================================
-- This script creates the foundational objects for the
-- autonomous, tool-using agent framework.
-- =============================================

-- 1. AgentTools Table
-------------------------------------------------
-- This table acts as a registry for all tools the agent can use.
-- The 'Description' is critical, as it's what the LLM uses
-- to decide which tool is appropriate for a given prompt.
-------------------------------------------------
IF OBJECT_ID('dbo.AgentTools', 'U') IS NOT NULL
    DROP TABLE dbo.AgentTools;

CREATE TABLE dbo.AgentTools (
    ToolId INT IDENTITY(1,1) PRIMARY KEY,
    ToolName NVARCHAR(128) NOT NULL UNIQUE,
    Description NVARCHAR(1024) NOT NULL,
    ObjectType NVARCHAR(128) NOT NULL, -- e.g., 'STORED_PROCEDURE', 'SCALAR_FUNCTION', 'TABLE_VALUED_FUNCTION'
    ObjectName NVARCHAR(256) NOT NULL,  -- e.g., 'dbo.sp_SomeTool'
    ParametersJson NVARCHAR(MAX),      -- A JSON schema describing the parameters
    IsEnabled BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
);

-- 2. sp_Converse Stored Procedure
-------------------------------------------------
-- This is the main entry point for the agent. It takes a user prompt,
-- uses an LLM to select a tool, executes the tool, and then uses
-- the LLM again to synthesize a final answer.
-------------------------------------------------
IF OBJECT_ID('dbo.sp_Converse', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Converse;

CREATE PROCEDURE dbo.sp_Converse
    @Prompt NVARCHAR(MAX),
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;








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
            -- The tool returns a table, so we need to capture its output.

             -- Simplified parameter passing

            -- Serialize the tool's output for the next step
            SELECT @ToolExecutionResult = (SELECT * FROM @AnalysisData FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
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
        

        SELECT TOP 1 @FinalAnswer = GeneratedText FROM @GenerationResult;

        -- Return the final answer
        SELECT @FinalAnswer AS FinalAnswer;

    END TRY
    BEGIN CATCH
        -- Error handling

        RAISERROR('An error occurred in the agent conversation: %s', 16, 1, @ErrorMessage);
    END CATCH;
END;

# 21 - Agent Framework: Dynamic Tool Selection and Autonomous Operation

This document provides the complete specification for Hartonomous's agent tools framework.

## Overview

**Traditional AI Agents**: External frameworks (LangChain, AutoGPT), brittle integrations
**Hartonomous Agents**: Native SQL table registry with dynamic stored procedure invocation

**Core Components**:
1. **AgentTools Table** - Registry of available procedures/functions
2. **Tool Selection Logic** - Query-based tool recommendation
3. **Dynamic Execution** - sp_executesql with JSON parameter binding
4. **OODA Integration** - Tool performance monitoring and optimization

---

## Part 1: The AgentTools Registry

### Schema

```sql
CREATE TABLE dbo.AgentTools (
    ToolId BIGINT IDENTITY PRIMARY KEY,
    ToolName NVARCHAR(200) NOT NULL UNIQUE,
    ToolCategory NVARCHAR(100) NOT NULL,  -- 'generation', 'reasoning', 'diagnostics', 'synthesis'
    Description NVARCHAR(2000) NOT NULL,
    ObjectType NVARCHAR(128) NOT NULL,    -- 'STORED_PROCEDURE', 'SCALAR_FUNCTION', 'TABLE_FUNCTION'
    ObjectName NVARCHAR(256) NOT NULL,    -- 'dbo.sp_SpatialNextToken', 'dbo.fn_ProjectTo3D'
    ParametersJson NVARCHAR(MAX),         -- JSON schema for parameters
    ReturnType NVARCHAR(200),             -- 'TABLE', 'SCALAR', 'NONE'
    IsEnabled BIT NOT NULL DEFAULT 1,
    SuccessRate FLOAT,                    -- Tracked by OODA loop
    AvgDurationMs FLOAT,                  -- Tracked by OODA loop
    TotalUsageCount BIGINT DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastUsedAt DATETIME2
);

CREATE NONCLUSTERED INDEX IX_AgentTools_Category
ON dbo.AgentTools (ToolCategory, IsEnabled) INCLUDE (ToolName, ObjectName);

CREATE NONCLUSTERED INDEX IX_AgentTools_Performance
ON dbo.AgentTools (SuccessRate DESC, AvgDurationMs ASC)
WHERE IsEnabled = 1;
```

### Seed Data

**Implementation**: `Seed_AgentTools.sql`

```sql
-- Generation Tools
INSERT INTO dbo.AgentTools (ToolName, ToolCategory, Description, ObjectType, ObjectName, ParametersJson, ReturnType)
VALUES
(
    'spatial_text_generation',
    'generation',
    'Generate text using spatial R-Tree navigation and O(log N) candidate selection',
    'STORED_PROCEDURE',
    'dbo.sp_SpatialNextToken',
    '{
        "prompt": {"type": "string", "required": true},
        "maxTokens": {"type": "int", "default": 100},
        "temperature": {"type": "float", "default": 0.7}
    }',
    'TABLE'
),
(
    'cross_modal_query',
    'generation',
    'Query across text, image, audio, video atoms in unified 3D semantic space',
    'STORED_PROCEDURE',
    'dbo.sp_CrossModalQuery',
    '{
        "queryText": {"type": "string", "required": true},
        "modalities": {"type": "array", "items": ["text", "image", "audio", "video"]},
        "topK": {"type": "int", "default": 50}
    }',
    'TABLE'
);

-- Reasoning Tools
INSERT INTO dbo.AgentTools (ToolName, ToolCategory, Description, ObjectType, ObjectName, ParametersJson, ReturnType)
VALUES
(
    'chain_of_thought',
    'reasoning',
    'Linear step-by-step reasoning with coherence analysis',
    'STORED_PROCEDURE',
    'dbo.sp_ChainOfThoughtReasoning',
    '{
        "prompt": {"type": "string", "required": true},
        "maxSteps": {"type": "int", "default": 10},
        "sessionId": {"type": "uniqueidentifier", "required": true}
    }',
    'TABLE'
),
(
    'tree_of_thought',
    'reasoning',
    'Explore N parallel reasoning paths, select best',
    'STORED_PROCEDURE',
    'dbo.sp_MultiPathReasoning',
    '{
        "prompt": {"type": "string", "required": true},
        "numPaths": {"type": "int", "default": 5},
        "maxStepsPerPath": {"type": "int", "default": 5},
        "sessionId": {"type": "uniqueidentifier", "required": true}
    }',
    'TABLE'
),
(
    'reflexion',
    'reasoning',
    'Generate N samples, find consensus answer via self-consistency',
    'STORED_PROCEDURE',
    'dbo.sp_SelfConsistencyReasoning',
    '{
        "prompt": {"type": "string", "required": true},
        "numSamples": {"type": "int", "default": 10},
        "sessionId": {"type": "uniqueidentifier", "required": true}
    }',
    'TABLE'
);

-- Synthesis Tools
INSERT INTO dbo.AgentTools (ToolName, ToolCategory, Description, ObjectType, ObjectName, ParametersJson, ReturnType)
VALUES
(
    'generate_audio',
    'synthesis',
    'Generate audio waveforms using geometric guidance and harmonic synthesis',
    'STORED_PROCEDURE',
    'dbo.sp_GenerateAudio',
    '{
        "prompt": {"type": "string", "required": true},
        "durationMs": {"type": "int", "default": 3000},
        "fundamentalHz": {"type": "float", "default": 440.0}
    }',
    'TABLE'
),
(
    'generate_image',
    'synthesis',
    'Generate image patches using geometric diffusion with spatial guidance',
    'STORED_PROCEDURE',
    'dbo.sp_GenerateImage',
    '{
        "prompt": {"type": "string", "required": true},
        "width": {"type": "int", "default": 512},
        "height": {"type": "int", "default": 512}
    }',
    'TABLE'
),
(
    'generate_video',
    'synthesis',
    'Generate video frames with motion vectors and PixelCloud geometry',
    'STORED_PROCEDURE',
    'dbo.sp_GenerateVideo',
    '{
        "prompt": {"type": "string", "required": true},
        "frameCount": {"type": "int", "default": 30},
        "fps": {"type": "int", "default": 30}
    }',
    'TABLE'
);

-- Diagnostics Tools
INSERT INTO dbo.AgentTools (ToolName, ToolCategory, Description, ObjectType, ObjectName, ParametersJson, ReturnType)
VALUES
(
    'analyze_system_state',
    'diagnostics',
    'Observe system metrics: query performance, index usage, cache hit rates',
    'STORED_PROCEDURE',
    'dbo.sp_Analyze',
    '{
        "analysisIntervalMinutes": {"type": "int", "default": 60}
    }',
    'NONE'
),
(
    'explain_inference',
    'diagnostics',
    'Explain how an inference result was generated (provenance trace)',
    'STORED_PROCEDURE',
    'dbo.sp_ExplainInference',
    '{
        "inferenceId": {"type": "uniqueidentifier", "required": true}
    }',
    'TABLE'
);
```

---

## Part 2: Tool Selection Logic

### Dynamic Tool Recommendation

**Stored Procedure**: `dbo.sp_SelectAgentTool`

```sql
CREATE PROCEDURE dbo.sp_SelectAgentTool
    @TaskDescription NVARCHAR(MAX),
    @RequiredCategory NVARCHAR(100) = NULL,
    @SessionId UNIQUEIDENTIFIER,
    @TenantId INT = 0
AS
BEGIN
    -- Embed task description to find semantically similar tools
    DECLARE @TaskEmbedding VARBINARY(MAX);

    EXEC dbo.sp_GenerateEmbedding
        @Text = @TaskDescription,
        @EmbeddingVector = @TaskEmbedding OUTPUT;

    -- Project to 3D geometry
    DECLARE @TaskGeometry GEOMETRY = dbo.fn_ProjectTo3D(@TaskEmbedding);

    -- Create tool description embeddings if not exists
    IF NOT EXISTS (SELECT 1 FROM dbo.AgentToolEmbeddings)
    BEGIN
        INSERT INTO dbo.AgentToolEmbeddings (ToolId, Description, DescriptionEmbedding, SpatialGeometry)
        SELECT
            ToolId,
            Description,
            dbo.fn_GenerateEmbedding(Description),
            dbo.fn_ProjectTo3D(dbo.fn_GenerateEmbedding(Description))
        FROM dbo.AgentTools
        WHERE IsEnabled = 1;
    END

    -- Find top 5 tools via spatial proximity
    SELECT TOP 5
        at.ToolId,
        at.ToolName,
        at.ToolCategory,
        at.Description,
        at.ObjectName,
        at.ParametersJson,
        at.SuccessRate,
        at.AvgDurationMs,
        ate.SpatialGeometry.STDistance(@TaskGeometry) AS SemanticDistance
    FROM dbo.AgentTools at
    INNER JOIN dbo.AgentToolEmbeddings ate ON at.ToolId = ate.ToolId
    WHERE at.IsEnabled = 1
        AND (@RequiredCategory IS NULL OR at.ToolCategory = @RequiredCategory)
        AND ate.SpatialGeometry.STIntersects(@TaskGeometry.STBuffer(50)) = 1
    ORDER BY
        ate.SpatialGeometry.STDistance(@TaskGeometry) ASC,
        at.SuccessRate DESC,
        at.AvgDurationMs ASC;
END
```

### Example: Selecting Tool for Task

```sql
DECLARE @SessionId UNIQUEIDENTIFIER = NEWID();

-- Task: "Generate creative story about space exploration"
EXEC dbo.sp_SelectAgentTool
    @TaskDescription = 'Generate creative story about space exploration',
    @RequiredCategory = NULL,  -- Let system choose category
    @SessionId = @SessionId;

-- Output:
-- Tool 1: spatial_text_generation (distance 2.3, success 0.94)
-- Tool 2: tree_of_thought (distance 5.7, success 0.89) -- Might help with creative branching
-- Tool 3: cross_modal_query (distance 8.2, success 0.92) -- Could fetch space images

-- Task: "Solve complex math problem step by step"
EXEC dbo.sp_SelectAgentTool
    @TaskDescription = 'Solve complex math problem step by step',
    @RequiredCategory = 'reasoning',  -- Force reasoning tools only
    @SessionId = @SessionId;

-- Output:
-- Tool 1: chain_of_thought (distance 1.5, success 0.91) -- Step-by-step is CoT
-- Tool 2: tree_of_thought (distance 4.2, success 0.88)
-- Tool 3: reflexion (distance 6.8, success 0.87)
```

---

## Part 3: Dynamic Tool Execution

### Parameter Binding from JSON Schema

**Function**: `dbo.fn_BindToolParameters`

```csharp
[SqlFunction(DataAccess = DataAccessKind.Read)]
public static SqlString BindToolParameters(
    SqlString parametersJson,
    SqlString userInputJson)
{
    var schema = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(parametersJson.Value);
    var userInput = JsonConvert.DeserializeObject<Dictionary<string, object>>(userInputJson.Value);

    var boundParams = new StringBuilder();

    foreach (var param in schema)
    {
        string paramName = param.Key;
        var paramDef = param.Value;

        object value;

        // Use user input if provided, otherwise use default
        if (userInput.ContainsKey(paramName))
        {
            value = userInput[paramName];
        }
        else if (paramDef.ContainsKey("default"))
        {
            value = paramDef["default"];
        }
        else if (paramDef.ContainsKey("required") && paramDef["required"] == true)
        {
            throw new InvalidOperationException($"Required parameter '{paramName}' not provided");
        }
        else
        {
            continue;  // Optional parameter not provided
        }

        // Format for T-SQL
        boundParams.Append($"@{paramName} = ");

        string paramType = paramDef["type"].ToString();
        switch (paramType)
        {
            case "string":
                boundParams.Append($"N'{value.ToString().Replace("'", "''")}'");
                break;
            case "int":
                boundParams.Append(value.ToString());
                break;
            case "float":
                boundParams.Append(value.ToString());
                break;
            case "uniqueidentifier":
                boundParams.Append($"'{value}'");
                break;
            default:
                boundParams.Append($"N'{value}'");
                break;
        }

        boundParams.Append(", ");
    }

    // Remove trailing comma
    if (boundParams.Length > 2)
        boundParams.Length -= 2;

    return new SqlString(boundParams.ToString());
}
```

### Execution Wrapper

**Stored Procedure**: `dbo.sp_ExecuteAgentTool`

```sql
CREATE PROCEDURE dbo.sp_ExecuteAgentTool
    @ToolId BIGINT,
    @UserInputJson NVARCHAR(MAX),  -- e.g., '{"prompt": "Hello world", "maxTokens": 50}'
    @SessionId UNIQUEIDENTIFIER,
    @TenantId INT = 0
AS
BEGIN
    -- Load tool metadata
    DECLARE @ObjectName NVARCHAR(256);
    DECLARE @ParametersJson NVARCHAR(MAX);
    DECLARE @ObjectType NVARCHAR(128);

    SELECT
        @ObjectName = ObjectName,
        @ParametersJson = ParametersJson,
        @ObjectType = ObjectType
    FROM dbo.AgentTools
    WHERE ToolId = @ToolId AND IsEnabled = 1;

    IF @ObjectName IS NULL
    BEGIN
        RAISERROR('Tool not found or disabled', 16, 1);
        RETURN;
    END

    -- Bind parameters
    DECLARE @BoundParams NVARCHAR(MAX) = dbo.fn_BindToolParameters(@ParametersJson, @UserInputJson);

    -- Build dynamic SQL
    DECLARE @SQL NVARCHAR(MAX);

    IF @ObjectType = 'STORED_PROCEDURE'
    BEGIN
        SET @SQL = 'EXEC ' + @ObjectName + ' ' + @BoundParams;
    END
    ELSE IF @ObjectType = 'SCALAR_FUNCTION'
    BEGIN
        SET @SQL = 'SELECT ' + @ObjectName + '(' + @BoundParams + ') AS Result';
    END
    ELSE IF @ObjectType = 'TABLE_FUNCTION'
    BEGIN
        SET @SQL = 'SELECT * FROM ' + @ObjectName + '(' + @BoundParams + ')';
    END

    -- Execute tool
    DECLARE @StartTime DATETIME2 = GETUTCDATE();
    DECLARE @Success BIT = 1;
    DECLARE @ErrorMessage NVARCHAR(MAX);

    BEGIN TRY
        EXEC sp_executesql @SQL;
    END TRY
    BEGIN CATCH
        SET @Success = 0;
        SET @ErrorMessage = ERROR_MESSAGE();
    END CATCH

    -- Track usage for OODA loop
    DECLARE @DurationMs FLOAT = DATEDIFF(MILLISECOND, @StartTime, GETUTCDATE());

    INSERT INTO dbo.AgentToolUsage (
        ToolId,
        SessionId,
        UserInputJson,
        Success,
        DurationMs,
        ErrorMessage,
        ExecutedAt
    )
    VALUES (
        @ToolId,
        @SessionId,
        @UserInputJson,
        @Success,
        @DurationMs,
        @ErrorMessage,
        @StartTime
    );

    -- Update tool stats (for OODA performance tracking)
    UPDATE dbo.AgentTools
    SET TotalUsageCount = TotalUsageCount + 1,
        LastUsedAt = GETUTCDATE()
    WHERE ToolId = @ToolId;

    -- Return error if failed
    IF @Success = 0
    BEGIN
        RAISERROR(@ErrorMessage, 16, 1);
    END
END
```

### Usage Example

```sql
DECLARE @SessionId UNIQUEIDENTIFIER = NEWID();

-- Step 1: Select tool
DECLARE @ToolId BIGINT = (
    SELECT TOP 1 ToolId
    FROM dbo.AgentTools
    WHERE ToolName = 'chain_of_thought'
);

-- Step 2: Execute with user input
EXEC dbo.sp_ExecuteAgentTool
    @ToolId = @ToolId,
    @UserInputJson = '{
        "prompt": "Solve: If x + 5 = 12, what is x?",
        "maxSteps": 5,
        "sessionId": "' + CAST(@SessionId AS NVARCHAR(36)) + '"
    }',
    @SessionId = @SessionId;

-- Result: sp_ChainOfThoughtReasoning executes with bound parameters
```

---

## Part 4: OODA Integration

### Performance Monitoring

**sp_Analyze** tracks tool usage and success rates:

```sql
-- Update tool performance metrics (runs hourly via OODA)
UPDATE at
SET SuccessRate = stats.SuccessRate,
    AvgDurationMs = stats.AvgDuration
FROM dbo.AgentTools at
INNER JOIN (
    SELECT
        ToolId,
        AVG(CAST(Success AS FLOAT)) AS SuccessRate,
        AVG(DurationMs) AS AvgDuration
    FROM dbo.AgentToolUsage
    WHERE ExecutedAt >= DATEADD(DAY, -7, GETUTCDATE())
    GROUP BY ToolId
) stats ON at.ToolId = stats.ToolId;
```

### Poor-Performing Tool Detection

**sp_Hypothesize** generates "OptimizeToolSelection" hypothesis:

```sql
-- Find tools with low success rates
DECLARE @poorTools NVARCHAR(MAX) = (
    SELECT ToolName, SuccessRate, TotalUsageCount
    FROM dbo.AgentTools
    WHERE IsEnabled = 1
        AND TotalUsageCount > 100  -- Sufficient sample size
        AND SuccessRate < 0.7       -- Low success threshold
    FOR JSON PATH
);

IF @poorTools IS NOT NULL
BEGIN
    INSERT INTO @HypothesisList (HypothesisType, Priority, Description, RequiredActions)
    VALUES (
        'OptimizeToolSelection',
        5,
        'Some agent tools have low success rates - recommend alternatives or disable',
        @poorTools
    );
END
```

### Auto-Disable Low-Performing Tools

**sp_Act** executes hypothesis:

```sql
-- Disable tools with consistent failure
UPDATE dbo.AgentTools
SET IsEnabled = 0
WHERE SuccessRate < 0.5
    AND TotalUsageCount > 200  -- Large sample
    AND ToolCategory NOT IN ('diagnostics');  -- Never disable diagnostics
```

---

## Part 5: Agent Decision Loop

### Complete Agent Flow

```sql
CREATE PROCEDURE dbo.sp_AgentExecuteTask
    @TaskDescription NVARCHAR(MAX),
    @SessionId UNIQUEIDENTIFIER,
    @TenantId INT = 0
AS
BEGIN
    -- Step 1: Select appropriate tool
    DECLARE @SelectedToolId BIGINT;

    SELECT TOP 1 @SelectedToolId = ToolId
    FROM dbo.sp_SelectAgentTool(@TaskDescription, NULL, @SessionId, @TenantId);

    IF @SelectedToolId IS NULL
    BEGIN
        RAISERROR('No suitable tool found for task', 16, 1);
        RETURN;
    END

    -- Step 2: Extract parameters from task description using NLP
    DECLARE @UserInputJson NVARCHAR(MAX);

    EXEC dbo.sp_ExtractParametersFromTask
        @TaskDescription = @TaskDescription,
        @ToolId = @SelectedToolId,
        @UserInputJson = @UserInputJson OUTPUT;

    -- Step 3: Execute tool
    EXEC dbo.sp_ExecuteAgentTool
        @ToolId = @SelectedToolId,
        @UserInputJson = @UserInputJson,
        @SessionId = @SessionId,
        @TenantId = @TenantId;

    -- Step 4: Store provenance in Neo4j
    EXEC dbo.sp_SyncAgentDecisionToNeo4j
        @SessionId = @SessionId,
        @TaskDescription = @TaskDescription,
        @SelectedToolId = @SelectedToolId,
        @UserInputJson = @UserInputJson;
END
```

### Provenance Tracking

**Neo4j Integration**:

```cypher
// Create agent decision node
CREATE (decision:AgentDecision {
    sessionId: $sessionId,
    taskDescription: $taskDescription,
    selectedToolId: $toolId,
    selectedToolName: $toolName,
    userInput: $userInputJson,
    decidedAt: datetime()
})

// Link to tool
MATCH (tool:AgentTool {toolId: $toolId})
CREATE (decision)-[:SELECTED_TOOL]->(tool)

// Link to resulting inference
MATCH (inference:Inference {sessionId: $sessionId})
WHERE inference.createdAt > decision.decidedAt
CREATE (decision)-[:RESULTED_IN]->(inference)
```

---

## Part 6: Adding New Tools

### Registration Process

```sql
-- Step 1: Create your stored procedure/function
CREATE PROCEDURE dbo.sp_CustomTool
    @Parameter1 NVARCHAR(MAX),
    @Parameter2 INT = 100
AS
BEGIN
    -- Your tool logic here
    SELECT 'Custom result' AS Output;
END
GO

-- Step 2: Register in AgentTools
INSERT INTO dbo.AgentTools (
    ToolName,
    ToolCategory,
    Description,
    ObjectType,
    ObjectName,
    ParametersJson,
    ReturnType
)
VALUES (
    'custom_tool',
    'diagnostics',  -- Choose category: generation, reasoning, diagnostics, synthesis
    'Description of what this tool does',
    'STORED_PROCEDURE',
    'dbo.sp_CustomTool',
    '{
        "parameter1": {"type": "string", "required": true, "description": "First param"},
        "parameter2": {"type": "int", "default": 100, "description": "Second param"}
    }',
    'TABLE'
);

-- Step 3: Generate embedding for tool description
INSERT INTO dbo.AgentToolEmbeddings (ToolId, Description, DescriptionEmbedding, SpatialGeometry)
SELECT
    ToolId,
    Description,
    dbo.fn_GenerateEmbedding(Description),
    dbo.fn_ProjectTo3D(dbo.fn_GenerateEmbedding(Description))
FROM dbo.AgentTools
WHERE ToolName = 'custom_tool';

-- Step 4: Tool is now available for agent selection
```

---

## Conclusion

**Hartonomous agent framework is not external libraries - it's SQL tables.**

✅ **AgentTools Registry**: Database table of available procedures/functions
✅ **Semantic Tool Selection**: Spatial proximity between task and tool descriptions
✅ **Dynamic Execution**: sp_executesql with JSON parameter binding
✅ **Performance Tracking**: Success rates, duration, usage counts
✅ **OODA Integration**: Automatic tool performance monitoring and optimization
✅ **Full Provenance**: Every agent decision tracked in Neo4j
✅ **Extensible**: Add new tools by inserting into AgentTools table

The agent is the database. Tool selection is a spatial query. Execution is sp_executesql. Performance optimization is the OODA loop.

This is autonomous operation as first-class database objects.

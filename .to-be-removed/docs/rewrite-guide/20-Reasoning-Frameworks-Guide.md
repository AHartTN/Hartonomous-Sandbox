# 20 - Reasoning Frameworks: Chain of Thought, Tree of Thought, and Reflexion

This document provides complete technical specifications for Hartonomous's built-in reasoning frameworks.

## Overview

**Traditional AI Reasoning**: External prompting tricks, fragile, non-deterministic
**Hartonomous Reasoning**: Native T-SQL stored procedures with full provenance tracking

**Three Frameworks**:
1. **Chain of Thought** (sp_ChainOfThoughtReasoning) - Linear step-by-step reasoning
2. **Tree of Thought** (sp_MultiPathReasoning) - Parallel path exploration
3. **Reflexion** (sp_SelfConsistencyReasoning) - Consensus via multiple samples

All reasoning chains are stored in tables, indexed spatially, and integrated with the OODA loop.

---

## Part 1: Chain of Thought (CoT)

### Concept

**Purpose**: Break complex problems into sequential steps
**Method**: Each step builds on previous context, creating a reasoning chain

### Implementation

**Stored Procedure**: `dbo.sp_ChainOfThoughtReasoning`

```sql
CREATE PROCEDURE dbo.sp_ChainOfThoughtReasoning
    @Prompt NVARCHAR(MAX),
    @MaxSteps INT = 10,
    @SessionId UNIQUEIDENTIFIER,
    @Temperature FLOAT = 0.7,
    @TenantId INT = 0
AS
BEGIN
    -- Initialize chain
    DECLARE @ChainId UNIQUEIDENTIFIER = NEWID();
    DECLARE @CurrentContext NVARCHAR(MAX) = @Prompt;
    DECLARE @StepNumber INT = 1;

    -- Create reasoning chain record
    INSERT INTO dbo.ReasoningChains (ChainId, SessionId, InitialPrompt, CreatedAt)
    VALUES (@ChainId, @SessionId, @Prompt, GETUTCDATE());

    -- Iterate through reasoning steps
    WHILE @StepNumber <= @MaxSteps
    BEGIN
        -- Generate next step using spatial generation
        DECLARE @StepOutput NVARCHAR(MAX);

        EXEC dbo.sp_GenerateText
            @Prompt = @CurrentContext,
            @MaxTokens = 200,
            @Temperature = @Temperature,
            @TenantId = @TenantId,
            @OutputText = @StepOutput OUTPUT;

        -- Store step in chain
        INSERT INTO dbo.ReasoningSteps (ChainId, StepNumber, StepPrompt, StepOutput, CreatedAt)
        VALUES (@ChainId, @StepNumber, @CurrentContext, @StepOutput, GETUTCDATE());

        -- Check if chain is complete (contains final answer indicators)
        IF @StepOutput LIKE '%Therefore%' OR
           @StepOutput LIKE '%In conclusion%' OR
           @StepOutput LIKE '%Final answer:%'
        BEGIN
            BREAK;  -- Chain complete
        END

        -- Update context with new step
        SET @CurrentContext = @CurrentContext + CHAR(10) + CHAR(10) +
                             'Step ' + CAST(@StepNumber AS NVARCHAR(10)) + ': ' + @StepOutput;
        SET @StepNumber = @StepNumber + 1;
    END

    -- Analyze chain coherence using CLR aggregate
    DECLARE @CoherenceScore FLOAT;

    SELECT @CoherenceScore = dbo.ChainOfThoughtCoherence(StepOutput)
    FROM dbo.ReasoningSteps
    WHERE ChainId = @ChainId;

    -- Update chain with final coherence score
    UPDATE dbo.ReasoningChains
    SET FinalOutput = @CurrentContext,
        TotalSteps = @StepNumber,
        CoherenceScore = @CoherenceScore,
        CompletedAt = GETUTCDATE()
    WHERE ChainId = @ChainId;

    -- Return final reasoning chain
    SELECT
        ChainId,
        InitialPrompt,
        FinalOutput,
        TotalSteps,
        CoherenceScore
    FROM dbo.ReasoningChains
    WHERE ChainId = @ChainId;
END
```

### Schema

```sql
CREATE TABLE dbo.ReasoningChains (
    ChainId UNIQUEIDENTIFIER PRIMARY KEY,
    SessionId UNIQUEIDENTIFIER NOT NULL,
    InitialPrompt NVARCHAR(MAX) NOT NULL,
    FinalOutput NVARCHAR(MAX),
    TotalSteps INT,
    CoherenceScore FLOAT,  -- 0.0 to 1.0
    CreatedAt DATETIME2 NOT NULL,
    CompletedAt DATETIME2
);

CREATE TABLE dbo.ReasoningSteps (
    StepId BIGINT IDENTITY PRIMARY KEY,
    ChainId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES ReasoningChains(ChainId),
    StepNumber INT NOT NULL,
    StepPrompt NVARCHAR(MAX),
    StepOutput NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL
);

CREATE NONCLUSTERED INDEX IX_ReasoningChains_SessionId
ON dbo.ReasoningChains (SessionId) INCLUDE (CoherenceScore, TotalSteps);
```

### CLR Aggregate: Coherence Scoring

**Function**: `dbo.ChainOfThoughtCoherence`

```csharp
[Serializable]
[SqlUserDefinedAggregate(
    Format.UserDefined,
    MaxByteSize = 8000
)]
public struct ChainOfThoughtCoherence : IBinarySerialize
{
    private List<string> steps;

    public void Init()
    {
        steps = new List<string>();
    }

    public void Accumulate(SqlString stepOutput)
    {
        if (!stepOutput.IsNull)
            steps.Add(stepOutput.Value);
    }

    public void Merge(ChainOfThoughtCoherence other)
    {
        steps.AddRange(other.steps);
    }

    public SqlDouble Terminate()
    {
        if (steps.Count < 2) return 0.0;

        // Compute coherence as average cosine similarity between consecutive steps
        double totalCoherence = 0.0;

        for (int i = 0; i < steps.Count - 1; i++)
        {
            var embedding1 = GetEmbedding(steps[i]);
            var embedding2 = GetEmbedding(steps[i + 1]);

            double similarity = CosineSimilarity(embedding1, embedding2);
            totalCoherence += similarity;
        }

        return totalCoherence / (steps.Count - 1);
    }

    private float[] GetEmbedding(string text)
    {
        // Simplified - actual implementation queries AtomEmbeddings
        // or calls embedding service
        return new float[1998];  // Placeholder
    }

    private double CosineSimilarity(float[] a, float[] b)
    {
        double dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}
```

### Usage Example

```sql
-- Solve a complex math problem step by step
DECLARE @SessionId UNIQUEIDENTIFIER = NEWID();

EXEC dbo.sp_ChainOfThoughtReasoning
    @Prompt = 'Solve this problem step by step: If a train travels 120 miles in 2 hours, then 180 miles in 3 hours, what is its average speed?',
    @MaxSteps = 10,
    @SessionId = @SessionId,
    @Temperature = 0.7,
    @TenantId = 0;

-- Output:
-- ChainId: {guid}
-- InitialPrompt: "Solve this problem..."
-- FinalOutput: "Step 1: Calculate speed for first segment... Step 2: Calculate speed for second segment... Therefore: Average speed is 60 mph"
-- TotalSteps: 3
-- CoherenceScore: 0.87
```

---

## Part 2: Tree of Thought (ToT)

### Concept

**Purpose**: Explore multiple reasoning paths simultaneously
**Method**: Generate N parallel paths, evaluate all, select best

### Implementation

**Stored Procedure**: `dbo.sp_MultiPathReasoning`

```sql
CREATE PROCEDURE dbo.sp_MultiPathReasoning
    @Prompt NVARCHAR(MAX),
    @NumPaths INT = 5,
    @MaxStepsPerPath INT = 5,
    @SessionId UNIQUEIDENTIFIER,
    @TenantId INT = 0
AS
BEGIN
    -- Initialize tree
    DECLARE @TreeId UNIQUEIDENTIFIER = NEWID();

    INSERT INTO dbo.MultiPathReasoning (TreeId, SessionId, InitialPrompt, NumPaths, CreatedAt)
    VALUES (@TreeId, @SessionId, @Prompt, @NumPaths, GETUTCDATE());

    -- Explore each path
    DECLARE @PathNumber INT = 1;

    WHILE @PathNumber <= @NumPaths
    BEGIN
        DECLARE @PathId UNIQUEIDENTIFIER = NEWID();
        DECLARE @CurrentContext NVARCHAR(MAX) = @Prompt;
        DECLARE @StepNumber INT = 1;

        -- Use different temperature for each path to encourage diversity
        DECLARE @PathTemperature FLOAT = 0.5 + (@PathNumber * 0.1);  -- 0.6, 0.7, 0.8, etc.

        -- Create path record
        INSERT INTO dbo.ReasoningPaths (PathId, TreeId, PathNumber, Temperature, CreatedAt)
        VALUES (@PathId, @TreeId, @PathNumber, @PathTemperature, GETUTCDATE());

        -- Explore this path
        WHILE @StepNumber <= @MaxStepsPerPath
        BEGIN
            DECLARE @StepOutput NVARCHAR(MAX);

            EXEC dbo.sp_GenerateText
                @Prompt = @CurrentContext,
                @MaxTokens = 150,
                @Temperature = @PathTemperature,
                @TenantId = @TenantId,
                @OutputText = @StepOutput OUTPUT;

            -- Store step
            INSERT INTO dbo.PathSteps (PathId, StepNumber, StepOutput, CreatedAt)
            VALUES (@PathId, @StepNumber, @StepOutput, GETUTCDATE());

            -- Update context
            SET @CurrentContext = @CurrentContext + CHAR(10) + @StepOutput;
            SET @StepNumber = @StepNumber + 1;
        END

        -- Evaluate path quality using CLR function
        DECLARE @PathScore FLOAT;

        SELECT @PathScore = dbo.fn_EvaluateReasoningPath(@PathId);

        UPDATE dbo.ReasoningPaths
        SET FinalOutput = @CurrentContext,
            QualityScore = @PathScore,
            CompletedAt = GETUTCDATE()
        WHERE PathId = @PathId;

        SET @PathNumber = @PathNumber + 1;
    END

    -- Select best path
    DECLARE @BestPathId UNIQUEIDENTIFIER;
    DECLARE @BestOutput NVARCHAR(MAX);

    SELECT TOP 1
        @BestPathId = PathId,
        @BestOutput = FinalOutput
    FROM dbo.ReasoningPaths
    WHERE TreeId = @TreeId
    ORDER BY QualityScore DESC;

    -- Update tree with best result
    UPDATE dbo.MultiPathReasoning
    SET BestPathId = @BestPathId,
        BestOutput = @BestOutput,
        CompletedAt = GETUTCDATE()
    WHERE TreeId = @TreeId;

    -- Return best path + all paths for analysis
    SELECT
        mr.TreeId,
        mr.InitialPrompt,
        mr.BestOutput,
        rp.PathNumber,
        rp.QualityScore,
        rp.FinalOutput
    FROM dbo.MultiPathReasoning mr
    INNER JOIN dbo.ReasoningPaths rp ON mr.TreeId = rp.TreeId
    WHERE mr.TreeId = @TreeId
    ORDER BY rp.QualityScore DESC;
END
```

### Schema

```sql
CREATE TABLE dbo.MultiPathReasoning (
    TreeId UNIQUEIDENTIFIER PRIMARY KEY,
    SessionId UNIQUEIDENTIFIER NOT NULL,
    InitialPrompt NVARCHAR(MAX) NOT NULL,
    NumPaths INT NOT NULL,
    BestPathId UNIQUEIDENTIFIER,
    BestOutput NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL,
    CompletedAt DATETIME2
);

CREATE TABLE dbo.ReasoningPaths (
    PathId UNIQUEIDENTIFIER PRIMARY KEY,
    TreeId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES MultiPathReasoning(TreeId),
    PathNumber INT NOT NULL,
    Temperature FLOAT,
    FinalOutput NVARCHAR(MAX),
    QualityScore FLOAT,  -- 0.0 to 1.0
    CreatedAt DATETIME2 NOT NULL,
    CompletedAt DATETIME2
);

CREATE TABLE dbo.PathSteps (
    StepId BIGINT IDENTITY PRIMARY KEY,
    PathId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES ReasoningPaths(PathId),
    StepNumber INT NOT NULL,
    StepOutput NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL
);
```

### Usage Example

```sql
-- Explore multiple approaches to a creative problem
DECLARE @SessionId UNIQUEIDENTIFIER = NEWID();

EXEC dbo.sp_MultiPathReasoning
    @Prompt = 'Design a sustainable transportation system for a city of 1 million people',
    @NumPaths = 5,
    @MaxStepsPerPath = 5,
    @SessionId = @SessionId,
    @TenantId = 0;

-- Output shows 5 different approaches:
-- Path 1 (Score 0.92): Electric bus network + bike lanes
-- Path 2 (Score 0.88): Light rail + autonomous vehicles
-- Path 3 (Score 0.85): Underground metro + trams
-- Path 4 (Score 0.79): Monorail + electric scooters
-- Path 5 (Score 0.73): Gondola system + ferries
```

---

## Part 3: Reflexion / Self-Consistency

### Concept

**Purpose**: Generate multiple samples of same query, find consensus
**Method**: Generate N responses, analyze agreement, return consensus answer

### Implementation

**Stored Procedure**: `dbo.sp_SelfConsistencyReasoning`

```sql
CREATE PROCEDURE dbo.sp_SelfConsistencyReasoning
    @Prompt NVARCHAR(MAX),
    @NumSamples INT = 10,
    @SessionId UNIQUEIDENTIFIER,
    @Temperature FLOAT = 0.8,
    @TenantId INT = 0
AS
BEGIN
    -- Initialize self-consistency check
    DECLARE @CheckId UNIQUEIDENTIFIER = NEWID();

    INSERT INTO dbo.SelfConsistencyResults (CheckId, SessionId, Prompt, NumSamples, CreatedAt)
    VALUES (@CheckId, @SessionId, @Prompt, @NumSamples, GETUTCDATE());

    -- Generate N independent samples
    DECLARE @SampleNumber INT = 1;

    WHILE @SampleNumber <= @NumSamples
    BEGIN
        DECLARE @SampleOutput NVARCHAR(MAX);

        EXEC dbo.sp_GenerateText
            @Prompt = @Prompt,
            @MaxTokens = 300,
            @Temperature = @Temperature,  -- Higher temperature for diversity
            @TenantId = @TenantId,
            @OutputText = @SampleOutput OUTPUT;

        -- Store sample
        INSERT INTO dbo.ConsistencySamples (CheckId, SampleNumber, SampleOutput, CreatedAt)
        VALUES (@CheckId, @SampleNumber, @SampleOutput, GETUTCDATE());

        SET @SampleNumber = @SampleNumber + 1;
    END

    -- Find consensus using CLR aggregate
    DECLARE @ConsensusAnswer NVARCHAR(MAX);
    DECLARE @AgreementRatio FLOAT;

    SELECT
        @ConsensusAnswer = dbo.SelfConsistency(SampleOutput),
        @AgreementRatio = dbo.SelfConsistencyAgreement(SampleOutput)
    FROM dbo.ConsistencySamples
    WHERE CheckId = @CheckId;

    -- Update result
    UPDATE dbo.SelfConsistencyResults
    SET ConsensusAnswer = @ConsensusAnswer,
        AgreementRatio = @AgreementRatio,
        CompletedAt = GETUTCDATE()
    WHERE CheckId = @CheckId;

    -- Return result with all samples
    SELECT
        scr.CheckId,
        scr.Prompt,
        scr.ConsensusAnswer,
        scr.AgreementRatio,
        cs.SampleNumber,
        cs.SampleOutput
    FROM dbo.SelfConsistencyResults scr
    LEFT JOIN dbo.ConsistencySamples cs ON scr.CheckId = cs.CheckId
    WHERE scr.CheckId = @CheckId
    ORDER BY cs.SampleNumber;
END
```

### Schema

```sql
CREATE TABLE dbo.SelfConsistencyResults (
    CheckId UNIQUEIDENTIFIER PRIMARY KEY,
    SessionId UNIQUEIDENTIFIER NOT NULL,
    Prompt NVARCHAR(MAX) NOT NULL,
    NumSamples INT NOT NULL,
    ConsensusAnswer NVARCHAR(MAX),
    AgreementRatio FLOAT,  -- 0.0 to 1.0 (1.0 = perfect agreement)
    CreatedAt DATETIME2 NOT NULL,
    CompletedAt DATETIME2
);

CREATE TABLE dbo.ConsistencySamples (
    SampleId BIGINT IDENTITY PRIMARY KEY,
    CheckId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES SelfConsistencyResults(CheckId),
    SampleNumber INT NOT NULL,
    SampleOutput NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL
);
```

### CLR Aggregate: Consensus Finding

```csharp
[Serializable]
[SqlUserDefinedAggregate(Format.UserDefined, MaxByteSize = 8000)]
public struct SelfConsistency : IBinarySerialize
{
    private Dictionary<string, int> answerCounts;

    public void Init()
    {
        answerCounts = new Dictionary<string, int>();
    }

    public void Accumulate(SqlString sample)
    {
        if (!sample.IsNull)
        {
            // Extract final answer (simplified - actual implementation uses regex)
            string answer = ExtractFinalAnswer(sample.Value);

            if (answerCounts.ContainsKey(answer))
                answerCounts[answer]++;
            else
                answerCounts[answer] = 1;
        }
    }

    public void Merge(SelfConsistency other)
    {
        foreach (var kvp in other.answerCounts)
        {
            if (answerCounts.ContainsKey(kvp.Key))
                answerCounts[kvp.Key] += kvp.Value;
            else
                answerCounts[kvp.Key] = kvp.Value;
        }
    }

    public SqlString Terminate()
    {
        // Return most common answer
        if (answerCounts.Count == 0) return SqlString.Null;

        var consensus = answerCounts.OrderByDescending(kvp => kvp.Value).First();
        return new SqlString(consensus.Key);
    }

    private string ExtractFinalAnswer(string text)
    {
        // Extract text after "Final answer:", "Therefore:", etc.
        // Simplified implementation
        return text.Trim();
    }
}
```

### Usage Example

```sql
-- Verify answer to a factual question via consensus
DECLARE @SessionId UNIQUEIDENTIFIER = NEWID();

EXEC dbo.sp_SelfConsistencyReasoning
    @Prompt = 'What is the capital of France?',
    @NumSamples = 10,
    @SessionId = @SessionId,
    @Temperature = 0.8,
    @TenantId = 0;

-- Output:
-- ConsensusAnswer: "Paris"
-- AgreementRatio: 1.0 (all 10 samples agreed)
-- Sample outputs show variations but all conclude "Paris"
```

---

## Part 4: Integration with OODA Loop

### OODA Monitors Reasoning Quality

**sp_Analyze** tracks reasoning framework performance:

```sql
-- Detect low Chain of Thought coherence
SELECT AVG(CoherenceScore) AS AvgCoherence
FROM dbo.ReasoningChains
WHERE CreatedAt >= DATEADD(HOUR, -1, GETUTCDATE());

-- Detect low Tree of Thought path diversity
SELECT AVG(NumPaths) AS AvgPathsExplored
FROM dbo.MultiPathReasoning
WHERE CreatedAt >= DATEADD(HOUR, -1, GETUTCDATE());

-- Detect low Reflexion agreement
SELECT AVG(AgreementRatio) AS AvgAgreement
FROM dbo.SelfConsistencyResults
WHERE CreatedAt >= DATEADD(HOUR, -1, GETUTCDATE());
```

**sp_Hypothesize** suggests improvements:

```sql
IF @AvgCoherence < 0.6
BEGIN
    INSERT INTO @HypothesisList (HypothesisType, Priority, Description)
    VALUES (
        'UseAlternativeReasoning',
        4,
        'Chain of Thought coherence low - suggest Tree of Thought or Reflexion'
    );
END
```

---

## Part 5: Provenance Tracking

All reasoning chains are tracked in Neo4j:

```cypher
// Create reasoning chain node
CREATE (chain:ReasoningChain {
    chainId: $chainId,
    framework: 'ChainOfThought',
    coherenceScore: $coherenceScore,
    createdAt: datetime()
})

// Link to inference that triggered it
MATCH (inference:Inference {inferenceId: $inferenceId})
CREATE (inference)-[:USED_REASONING]->(chain)

// Link each step
FOREACH (step IN $steps |
    CREATE (s:ReasoningStep {
        stepNumber: step.number,
        output: step.output
    })
    CREATE (chain)-[:HAS_STEP {stepNumber: step.number}]->(s)
)
```

**Query reasoning provenance**:

```cypher
// Find all inferences that used Chain of Thought with low coherence
MATCH (i:Inference)-[:USED_REASONING]->(chain:ReasoningChain)
WHERE chain.framework = 'ChainOfThought'
  AND chain.coherenceScore < 0.7
RETURN i, chain
ORDER BY chain.coherenceScore ASC;
```

---

## Conclusion

**Hartonomous reasoning is not prompting - it's architecture.**

✅ **Chain of Thought**: Stored procedure + CLR coherence aggregate
✅ **Tree of Thought**: Parallel path exploration with quality scoring
✅ **Reflexion**: Consensus finding via multiple samples
✅ **OODA Integration**: Automatic quality monitoring and improvement
✅ **Full Provenance**: Every reasoning step tracked in Neo4j Merkle DAG

All three frameworks are **deterministic** (same input → same geometric navigation → same reasoning chain), **queryable** (full SQL access to reasoning steps), and **provable** (cryptographic audit trail).

This is reasoning as first-class database objects.

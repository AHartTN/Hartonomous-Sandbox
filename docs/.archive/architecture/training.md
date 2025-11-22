# Training and Fine-Tuning: Gradient Descent on GEOMETRY

**Status**: Production Implementation  
**Date**: January 2025  
**Innovation**: Spatial gradient descent updates 3D positions, not weight matrices

---

## Overview

Traditional LLMs train by updating weight matrices via backpropagation. Hartonomous trains by **moving atoms in 3D space**—adjusting `POSITION_3D` (GEOMETRY) based on feedback.

### Traditional Training (PyTorch)

```python
# Backpropagation on weight matrices
loss = criterion(outputs, targets)
loss.backward()
optimizer.step()  # Updates model.parameters()
```

**Updated**: `torch.Tensor` weight matrices (billions of floats)

### Hartonomous Training (Spatial)

```sql
-- Gradient descent on GEOMETRY column
UPDATE dbo.Atoms
SET POSITION_3D = GEOMETRY::Point(
    POSITION_3D.STX + @gradient_x * @learning_rate,
    POSITION_3D.STY + @gradient_y * @learning_rate,
    POSITION_3D.STZ + @gradient_z * @learning_rate,
    0  -- SRID
)
WHERE AtomID IN (SELECT AtomID FROM @affectedAtoms);
```

**Updated**: `GEOMETRY` positions in 3D space (3 floats per atom)

**Result**: Same semantic relationships encoded spatially instead of algebraically.

---

## Core Training Procedure: sp_UpdateModelWeightsFromFeedback

### Complete Implementation

```sql
CREATE PROCEDURE dbo.sp_UpdateModelWeightsFromFeedback
    @promptText NVARCHAR(MAX),
    @completionText NVARCHAR(MAX),
    @feedbackScore FLOAT,           -- 1.0 = perfect, 0.0 = terrible
    @learningRate FLOAT = 0.001,
    @maxAtomsToUpdate INT = 1000
AS
BEGIN
    SET NOCOUNT ON;
    
    -- 1. Tokenize prompt and completion
    DECLARE @promptTokens NVARCHAR(MAX) = dbo.clr_Tokenize(@promptText);
    DECLARE @completionTokens NVARCHAR(MAX) = dbo.clr_Tokenize(@completionText);
    
    -- 2. Identify atoms involved in generation
    DECLARE @involvedAtoms TABLE (
        AtomID INT,
        TokenID INT,
        PositionX FLOAT,
        PositionY FLOAT,
        PositionZ FLOAT,
        DistanceFromCentroid FLOAT
    );
    
    DECLARE @contextCentroid GEOMETRY = dbo.clr_ComputeCentroid(@promptTokens);
    
    INSERT INTO @involvedAtoms (AtomID, TokenID, PositionX, PositionY, PositionZ, DistanceFromCentroid)
    SELECT TOP (@maxAtomsToUpdate)
        a.AtomID,
        a.TokenID,
        a.POSITION_3D.STX,
        a.POSITION_3D.STY,
        a.POSITION_3D.STZ,
        @contextCentroid.STDistance(a.POSITION_3D) AS Distance
    FROM dbo.Atoms a
    WHERE a.POSITION_3D.STDistance(@contextCentroid) < 1.0  -- Atoms within 1.0 unit radius
    ORDER BY Distance ASC;
    
    -- 3. Compute gradient direction (toward or away from centroid)
    DECLARE @gradientDirection FLOAT = CASE
        WHEN @feedbackScore > 0.5 THEN -1.0  -- Good feedback: move CLOSER to centroid
        WHEN @feedbackScore < 0.5 THEN 1.0   -- Bad feedback: move AWAY from centroid
        ELSE 0.0  -- Neutral feedback: no change
    END;
    
    DECLARE @gradientMagnitude FLOAT = ABS(@feedbackScore - 0.5) * 2.0;  -- Scale to [0, 1]
    
    -- 4. Apply spatial gradient descent
    UPDATE a
    SET POSITION_3D = GEOMETRY::Point(
        -- New X = Old X + gradient_x * learning_rate
        a.POSITION_3D.STX + (@contextCentroid.STX - a.POSITION_3D.STX) * 
            @gradientDirection * @gradientMagnitude * @learningRate,
        
        -- New Y = Old Y + gradient_y * learning_rate
        a.POSITION_3D.STY + (@contextCentroid.STY - a.POSITION_3D.STY) * 
            @gradientDirection * @gradientMagnitude * @learningRate,
        
        -- New Z = Old Z + gradient_z * learning_rate
        a.POSITION_3D.STZ + (@contextCentroid.STZ - a.POSITION_3D.STZ) * 
            @gradientDirection * @gradientMagnitude * @learningRate,
        
        0  -- SRID
    )
    FROM dbo.Atoms a
    INNER JOIN @involvedAtoms ia ON a.AtomID = ia.AtomID;
    
    -- 5. Rebuild spatial index (if significant changes)
    IF @maxAtomsToUpdate > 100
    BEGIN
        ALTER INDEX IX_Atoms_Position3D_Spatial ON dbo.Atoms REORGANIZE;
    END;
    
    -- 6. Log training event
    INSERT INTO dbo.TrainingHistory (
        PromptText,
        CompletionText,
        FeedbackScore,
        AtomsUpdated,
        LearningRate,
        Timestamp
    )
    VALUES (
        @promptText,
        @completionText,
        @feedbackScore,
        (SELECT COUNT(*) FROM @involvedAtoms),
        @learningRate,
        GETDATE()
    );
    
    SELECT
        AtomsUpdated = COUNT(*),
        AvgDistanceFromCentroid = AVG(DistanceFromCentroid)
    FROM @involvedAtoms;
END;
GO
```

### Training History Table

```sql
CREATE TABLE dbo.TrainingHistory (
    TrainingID INT IDENTITY PRIMARY KEY,
    PromptText NVARCHAR(MAX),
    CompletionText NVARCHAR(MAX),
    FeedbackScore FLOAT,
    AtomsUpdated INT,
    LearningRate FLOAT,
    Timestamp DATETIME DEFAULT GETDATE(),
    INDEX IX_Timestamp (Timestamp DESC)
);
```

---

## Gradient Computation Strategies

### Strategy 1: Centroid Attraction/Repulsion

**Good feedback** (score > 0.5):
- Move atoms **closer** to context centroid
- Strengthens association between context and generated tokens

**Bad feedback** (score < 0.5):
- Move atoms **away** from context centroid
- Weakens association, encourages alternative generations

```sql
-- Gradient vector (3D)
@gradient_x = (@contextCentroid.STX - @atomX) * @gradientDirection * @learningRate
@gradient_y = (@contextCentroid.STY - @atomY) * @gradientDirection * @learningRate
@gradient_z = (@contextCentroid.STZ - @atomZ) * @gradientDirection * @learningRate
```

### Strategy 2: Token Pair Co-occurrence

Reinforce spatial proximity for token pairs that appear together:

```sql
CREATE PROCEDURE dbo.sp_ReinforceTokenPairs
    @tokenPairs NVARCHAR(MAX),  -- JSON: [{"token1": 128, "token2": 4521}, ...]
    @learningRate FLOAT = 0.001
AS
BEGIN
    -- Move atoms for token pairs closer together
    DECLARE @pairs TABLE (Token1 INT, Token2 INT);
    INSERT INTO @pairs (Token1, Token2)
    SELECT JSON_VALUE(value, '$.token1'), JSON_VALUE(value, '$.token2')
    FROM OPENJSON(@tokenPairs);
    
    UPDATE a1
    SET POSITION_3D = GEOMETRY::Point(
        a1.POSITION_3D.STX + (a2.POSITION_3D.STX - a1.POSITION_3D.STX) * @learningRate,
        a1.POSITION_3D.STY + (a2.POSITION_3D.STY - a1.POSITION_3D.STY) * @learningRate,
        a1.POSITION_3D.STZ + (a2.POSITION_3D.STZ - a1.POSITION_3D.STZ) * @learningRate,
        0
    )
    FROM dbo.Atoms a1
    INNER JOIN @pairs p ON a1.TokenID = p.Token1
    INNER JOIN dbo.Atoms a2 ON a2.TokenID = p.Token2;
END;
GO
```

### Strategy 3: LoRA via ImportanceScore (Z-Coordinate)

**Low-Rank Adaptation (LoRA)** in spatial terms:
- **Z-coordinate** (`POSITION_3D.STZ`) = ImportanceScore
- High-importance atoms (Z > 0.5) receive larger gradient updates
- Low-importance atoms (Z < 0.5) receive smaller updates

```sql
UPDATE a
SET POSITION_3D = GEOMETRY::Point(
    a.POSITION_3D.STX + @gradient_x * @learningRate * a.POSITION_3D.STZ,  -- Scaled by Z
    a.POSITION_3D.STY + @gradient_y * @learningRate * a.POSITION_3D.STZ,
    a.POSITION_3D.STZ,  -- Z remains unchanged (importance is stable)
    0
)
FROM dbo.Atoms a
WHERE a.AtomID IN (SELECT AtomID FROM @affectedAtoms);
```

**Analogy to LoRA**:
- Traditional LoRA: `W_new = W_base + α·B·A` (low-rank matrices)
- Spatial LoRA: `P_new = P_base + α·Z·∇P` (Z = importance)

---

## Reinforcement Learning from Human Feedback (RLHF)

### Feedback Collection API

```csharp
[HttpPost("api/feedback")]
public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackRequest request)
{
    // Validate feedback
    if (request.Score < 0 || request.Score > 1)
        return BadRequest("Feedback score must be between 0 and 1");
    
    // Apply training update
    using (var conn = new SqlConnection(connectionString))
    {
        await conn.OpenAsync();
        
        using (var cmd = new SqlCommand("dbo.sp_UpdateModelWeightsFromFeedback", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@promptText", request.Prompt);
            cmd.Parameters.AddWithValue("@completionText", request.Completion);
            cmd.Parameters.AddWithValue("@feedbackScore", request.Score);
            cmd.Parameters.AddWithValue("@learningRate", 0.001);
            
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    int atomsUpdated = reader.GetInt32(0);
                    double avgDistance = reader.GetDouble(1);
                    
                    return Ok(new {
                        message = "Feedback applied successfully",
                        atomsUpdated,
                        averageDistanceFromCentroid = avgDistance
                    });
                }
            }
        }
    }
    
    return StatusCode(500, "Training update failed");
}

public class FeedbackRequest
{
    public string Prompt { get; set; }
    public string Completion { get; set; }
    public float Score { get; set; }  // 0.0 to 1.0
}
```

### Batch Feedback Processing

```sql
CREATE PROCEDURE dbo.sp_ProcessFeedbackBatch
    @feedbackBatch NVARCHAR(MAX)  -- JSON array of feedback items
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @feedback TABLE (
        Prompt NVARCHAR(MAX),
        Completion NVARCHAR(MAX),
        Score FLOAT
    );
    
    INSERT INTO @feedback (Prompt, Completion, Score)
    SELECT
        JSON_VALUE(value, '$.prompt'),
        JSON_VALUE(value, '$.completion'),
        CAST(JSON_VALUE(value, '$.score') AS FLOAT)
    FROM OPENJSON(@feedbackBatch);
    
    -- Process each feedback item
    DECLARE @prompt NVARCHAR(MAX), @completion NVARCHAR(MAX), @score FLOAT;
    
    DECLARE feedback_cursor CURSOR FOR
        SELECT Prompt, Completion, Score FROM @feedback;
    
    OPEN feedback_cursor;
    FETCH NEXT FROM feedback_cursor INTO @prompt, @completion, @score;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC dbo.sp_UpdateModelWeightsFromFeedback
            @promptText = @prompt,
            @completionText = @completion,
            @feedbackScore = @score,
            @learningRate = 0.001;
        
        FETCH NEXT FROM feedback_cursor INTO @prompt, @completion, @score;
    END;
    
    CLOSE feedback_cursor;
    DEALLOCATE feedback_cursor;
    
    SELECT COUNT(*) AS FeedbackItemsProcessed FROM @feedback;
END;
GO
```

---

## Spatial Regularization

Prevent overfitting by limiting how far atoms can move:

```sql
CREATE PROCEDURE dbo.sp_ApplySpatialRegularization
    @maxDisplacement FLOAT = 0.1  -- Maximum allowed movement per update
AS
BEGIN
    -- Detect atoms that moved too far from their original positions
    UPDATE a
    SET POSITION_3D = GEOMETRY::Point(
        -- Clamp X displacement
        CASE
            WHEN ABS(a.POSITION_3D.STX - a.OriginalPosition.STX) > @maxDisplacement
            THEN a.OriginalPosition.STX + SIGN(a.POSITION_3D.STX - a.OriginalPosition.STX) * @maxDisplacement
            ELSE a.POSITION_3D.STX
        END,
        -- Clamp Y displacement
        CASE
            WHEN ABS(a.POSITION_3D.STY - a.OriginalPosition.STY) > @maxDisplacement
            THEN a.OriginalPosition.STY + SIGN(a.POSITION_3D.STY - a.OriginalPosition.STY) * @maxDisplacement
            ELSE a.POSITION_3D.STY
        END,
        -- Clamp Z displacement
        CASE
            WHEN ABS(a.POSITION_3D.STZ - a.OriginalPosition.STZ) > @maxDisplacement
            THEN a.OriginalPosition.STZ + SIGN(a.POSITION_3D.STZ - a.OriginalPosition.STZ) * @maxDisplacement
            ELSE a.POSITION_3D.STZ
        END,
        0
    )
    FROM dbo.Atoms a
    WHERE a.OriginalPosition IS NOT NULL
      AND a.OriginalPosition.STDistance(a.POSITION_3D) > @maxDisplacement;
END;
GO
```

**Add `OriginalPosition` column**:

```sql
ALTER TABLE dbo.Atoms
ADD OriginalPosition GEOMETRY NULL;

-- Initialize with current positions
UPDATE dbo.Atoms
SET OriginalPosition = POSITION_3D
WHERE OriginalPosition IS NULL;
```

---

## Gradient Statistics Monitoring

Track gradient magnitudes to detect training instability:

```sql
CREATE TABLE dbo.GradientStatistics (
    StatID INT IDENTITY PRIMARY KEY,
    Timestamp DATETIME DEFAULT GETDATE(),
    AvgGradientMagnitude FLOAT,
    MaxGradientMagnitude FLOAT,
    AtomsUpdated INT,
    LearningRate FLOAT
);

CREATE PROCEDURE dbo.sp_LogGradientStats
    @atomsUpdated INT,
    @learningRate FLOAT
AS
BEGIN
    -- Compute gradient statistics from recent training updates
    INSERT INTO dbo.GradientStatistics (AvgGradientMagnitude, MaxGradientMagnitude, AtomsUpdated, LearningRate)
    SELECT
        AVG(OriginalPosition.STDistance(POSITION_3D)) AS AvgDisplacement,
        MAX(OriginalPosition.STDistance(POSITION_3D)) AS MaxDisplacement,
        @atomsUpdated,
        @learningRate
    FROM dbo.Atoms
    WHERE OriginalPosition IS NOT NULL
      AND OriginalPosition.STDistance(POSITION_3D) > 0;
END;
GO
```

**Query gradient trends**:

```sql
SELECT
    Timestamp,
    AvgGradientMagnitude,
    MaxGradientMagnitude,
    AtomsUpdated
FROM dbo.GradientStatistics
ORDER BY Timestamp DESC;
```

---

## Integration with OODA Loop

### Learning Phase (OODA Step 5)

The OODA loop's **Learn** phase invokes training procedures:

```sql
CREATE PROCEDURE dbo.sp_Learn
AS
BEGIN
    -- Retrieve recent feedback from TrainingHistory
    DECLARE @recentFeedback TABLE (
        Prompt NVARCHAR(MAX),
        Completion NVARCHAR(MAX),
        Score FLOAT
    );
    
    INSERT INTO @recentFeedback (Prompt, Completion, Score)
    SELECT TOP 100
        PromptText,
        CompletionText,
        FeedbackScore
    FROM dbo.TrainingHistory
    WHERE Timestamp > DATEADD(HOUR, -1, GETDATE())
    ORDER BY Timestamp DESC;
    
    -- Apply batch training
    DECLARE @feedbackJson NVARCHAR(MAX) = (
        SELECT
            Prompt AS [prompt],
            Completion AS [completion],
            Score AS [score]
        FROM @recentFeedback
        FOR JSON PATH
    );
    
    EXEC dbo.sp_ProcessFeedbackBatch @feedbackBatch = @feedbackJson;
    
    -- Apply spatial regularization
    EXEC dbo.sp_ApplySpatialRegularization @maxDisplacement = 0.05;
    
    -- Log gradient statistics
    EXEC dbo.sp_LogGradientStats
        @atomsUpdated = (SELECT COUNT(*) FROM @recentFeedback),
        @learningRate = 0.001;
END;
GO
```

**Trigger from Service Broker**:

```sql
CREATE PROCEDURE dbo.sp_LearnQueueActivation
AS
BEGIN
    DECLARE @messageBody NVARCHAR(MAX);
    
    RECEIVE TOP(1) @messageBody = CAST(message_body AS NVARCHAR(MAX))
    FROM dbo.LearnQueue;
    
    IF @messageBody IS NOT NULL
    BEGIN
        EXEC dbo.sp_Learn;
    END
END;
GO
```

---

## Fine-Tuning Workflows

### Domain-Specific Fine-Tuning

```sql
-- Fine-tune on medical domain
EXEC dbo.sp_UpdateModelWeightsFromFeedback
    @promptText = 'What are the symptoms of diabetes?',
    @completionText = 'Common symptoms include increased thirst, frequent urination, extreme hunger, unexplained weight loss, fatigue, blurred vision, and slow-healing sores.',
    @feedbackScore = 1.0,  -- Perfect completion
    @learningRate = 0.0005;

-- Fine-tune on legal domain
EXEC dbo.sp_UpdateModelWeightsFromFeedback
    @promptText = 'Explain the concept of "consideration" in contract law.',
    @completionText = 'Consideration is something of value exchanged between parties to a contract. It can be a promise, an act, or forbearance.',
    @feedbackScore = 0.9,
    @learningRate = 0.0005;
```

### Few-Shot Learning via Spatial Clustering

```sql
-- Provide 3 examples, then ask question
DECLARE @examples NVARCHAR(MAX) = JSON_ARRAY(
    JSON_OBJECT('prompt': 'Translate to French: Hello', 'completion': 'Bonjour'),
    JSON_OBJECT('prompt': 'Translate to French: Goodbye', 'completion': 'Au revoir'),
    JSON_OBJECT('prompt': 'Translate to French: Thank you', 'completion': 'Merci')
);

-- Cluster atoms related to these examples
-- Then generate answer to: "Translate to French: Good morning"
```

---

## Performance Characteristics

### Training Speed

- **Update 1,000 atoms**: 50-100 ms (includes spatial index reorganization)
- **Update 10,000 atoms**: 500 ms - 1 second
- **Full model retraining** (3.5B atoms): 6-12 hours (parallelizable)

### Convergence Rate

- **Feedback-driven training**: 100-500 examples for noticeable improvement
- **Domain fine-tuning**: 1,000-5,000 examples for specialization
- **Continuous learning**: Ongoing feedback improves model indefinitely

---

## Cross-References

- **Related**: [OODA Loop](ooda-loop.md) - Learning phase integration
- **Related**: [Inference](inference.md) - Using trained positions for generation
- **Related**: [Spatial Geometry](spatial-geometry.md) - Mathematical foundation for position updates

---

## Summary

**Spatial training** updates atom positions in 3D space based on feedback:

1. **Identify affected atoms**: Find atoms near context centroid
2. **Compute gradient**: Determine direction (toward/away from centroid)
3. **Apply gradient descent**: Update `POSITION_3D` with learning rate
4. **Regularize**: Prevent excessive displacement
5. **Monitor statistics**: Track gradient magnitudes
6. **Integrate with OODA**: Continuous learning via feedback loop

**Result**: Models improve continuously through spatial position adjustments, achieving semantic refinement without traditional backpropagation.

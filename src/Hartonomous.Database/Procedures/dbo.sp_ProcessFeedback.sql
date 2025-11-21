-- sp_ProcessFeedback: Process user feedback to adjust relationship weights for RLHF
-- Part of Phase 2: Cognitive Implementation - Feedback Loop
-- Integrates with OODA Loop learning phase

CREATE PROCEDURE dbo.sp_ProcessFeedback
    @InferenceId BIGINT,
    @Rating INT,                    -- 1-5 scale
    @Comments NVARCHAR(2000) = NULL,
    @UserId NVARCHAR(128) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Success BIT = 0;
    DECLARE @Message NVARCHAR(500);
    DECLARE @AffectedRelations INT = 0;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Validate rating
        IF @Rating < 1 OR @Rating > 5
        BEGIN
            SET @Message = 'Rating must be between 1 and 5';
            RAISERROR(@Message, 16, 1);
            RETURN;
        END

        -- Get the inference request details
        DECLARE @SessionId BIGINT;
        DECLARE @TenantId INT;
        DECLARE @InferenceType NVARCHAR(50);

        SELECT @SessionId = SessionId,
               @TenantId = TenantId,
               @InferenceType = InferenceType
        FROM dbo.InferenceRequests
        WHERE InferenceRequestId = @InferenceId;

        IF @SessionId IS NULL
        BEGIN
            SET @Message = 'Inference request not found';
            RAISERROR(@Message, 16, 1);
            RETURN;
        END

        -- Store the feedback
        INSERT INTO dbo.InferenceFeedback (
            InferenceRequestId,
            Rating,
            Comments,
            UserId,
            FeedbackTimestamp
        )
        VALUES (
            @InferenceId,
            @Rating,
            @Comments,
            @UserId,
            SYSUTCDATETIME()
        );

        -- Calculate weight adjustment based on rating
        -- Rating 1-2: Decrease weight (negative feedback)
        -- Rating 3: Neutral (no change)
        -- Rating 4-5: Increase weight (positive feedback)
        DECLARE @WeightAdjustment FLOAT;
        SET @WeightAdjustment = CASE
            WHEN @Rating = 1 THEN -0.15
            WHEN @Rating = 2 THEN -0.08
            WHEN @Rating = 3 THEN 0.0
            WHEN @Rating = 4 THEN 0.05
            WHEN @Rating = 5 THEN 0.10
        END;

        -- Update weights on AtomRelation for atoms used in this inference
        -- This implements RLHF by adjusting the semantic graph edges
        IF @WeightAdjustment <> 0
        BEGIN
            UPDATE ar
            SET ar.Weight = GREATEST(0.01, LEAST(1.0, ar.Weight + @WeightAdjustment)),
                ar.UpdatedAt = SYSUTCDATETIME()
            FROM dbo.AtomRelation ar
            INNER JOIN dbo.InferenceAtomUsage iau ON ar.AtomRelationId = iau.AtomRelationId
            WHERE iau.InferenceRequestId = @InferenceId;

            SET @AffectedRelations = @@ROWCOUNT;

            -- Log the weight adjustment for auditing
            INSERT INTO dbo.AutonomousImprovementHistory (
                ImprovementType,
                TargetEntity,
                TargetId,
                OldValue,
                NewValue,
                RiskLevel,
                ApprovedBy,
                ExecutedAt,
                Success,
                Notes
            )
            VALUES (
                'FeedbackWeightAdjustment',
                'AtomRelation',
                @InferenceId,
                CAST(@WeightAdjustment AS NVARCHAR(50)),
                CAST(@AffectedRelations AS NVARCHAR(50)) + ' relations updated',
                CASE WHEN ABS(@WeightAdjustment) > 0.1 THEN 'Medium' ELSE 'Low' END,
                COALESCE(@UserId, 'SYSTEM'),
                SYSUTCDATETIME(),
                1,
                CONCAT('Feedback Rating: ', @Rating, ', Comments: ', LEFT(COALESCE(@Comments, 'N/A'), 200))
            );
        END

        -- If very negative feedback (rating 1), flag for review
        IF @Rating = 1
        BEGIN
            INSERT INTO dbo.PendingActions (
                ActionType,
                TargetEntity,
                TargetId,
                Description,
                Priority,
                Status,
                CreatedAt,
                Metadata
            )
            VALUES (
                'ReviewNegativeFeedback',
                'InferenceRequest',
                @InferenceId,
                CONCAT('User reported very poor inference quality. Comments: ', LEFT(COALESCE(@Comments, 'No comments'), 500)),
                'High',
                'Pending',
                SYSUTCDATETIME(),
                JSON_OBJECT('rating': @Rating, 'userId': @UserId, 'sessionId': @SessionId)
            );
        END

        -- Update inference request with feedback summary
        UPDATE dbo.InferenceRequests
        SET UserFeedback = CONCAT('Rating: ', @Rating, ' | ', COALESCE(@Comments, 'No comments')),
            UpdatedAt = SYSUTCDATETIME()
        WHERE InferenceRequestId = @InferenceId;

        COMMIT TRANSACTION;

        SET @Success = 1;
        SET @Message = CONCAT('Feedback processed successfully. ', @AffectedRelations, ' relationship weights adjusted.');

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @Success = 0;
        SET @Message = ERROR_MESSAGE();

        -- Log error
        INSERT INTO dbo.AutonomousImprovementHistory (
            ImprovementType,
            TargetEntity,
            TargetId,
            RiskLevel,
            ExecutedAt,
            Success,
            ErrorMessage
        )
        VALUES (
            'FeedbackProcessingError',
            'InferenceRequest',
            @InferenceId,
            'Low',
            SYSUTCDATETIME(),
            0,
            @Message
        );
    END CATCH

    -- Return result
    SELECT @Success AS Success,
           @Message AS Message,
           @AffectedRelations AS AffectedRelations;
END;
GO

-- Create feedback table if not exists
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.InferenceFeedback') AND type = 'U')
BEGIN
    CREATE TABLE dbo.InferenceFeedback (
        FeedbackId BIGINT IDENTITY(1,1) PRIMARY KEY,
        InferenceRequestId BIGINT NOT NULL,
        Rating INT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
        Comments NVARCHAR(2000) NULL,
        UserId NVARCHAR(128) NULL,
        FeedbackTimestamp DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_InferenceFeedback_InferenceRequest
            FOREIGN KEY (InferenceRequestId) REFERENCES dbo.InferenceRequests(InferenceRequestId)
    );

    CREATE INDEX IX_InferenceFeedback_InferenceRequestId
        ON dbo.InferenceFeedback(InferenceRequestId);
    CREATE INDEX IX_InferenceFeedback_Rating
        ON dbo.InferenceFeedback(Rating);
END;
GO

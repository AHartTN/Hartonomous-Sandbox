-- =============================================
-- Stored Procedure: sp_ComputeSemanticFeatures
-- Description: Computes semantic features for a single atom embedding
-- Used by sp_ComputeAllSemanticFeatures for batch processing
-- =============================================
CREATE PROCEDURE dbo.sp_ComputeSemanticFeatures
    @atom_embedding_id BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @embedding_vector VARBINARY(MAX);
    DECLARE @atom_id BIGINT;
    DECLARE @canonical_text NVARCHAR(MAX);
    
    -- Retrieve embedding data
    SELECT 
        @atom_id = ae.AtomId,
        @canonical_text = a.CanonicalText
    FROM dbo.AtomEmbedding ae
    INNER JOIN dbo.Atom a ON ae.AtomId = a.AtomId
    WHERE ae.AtomEmbeddingId = @atom_embedding_id;
    
    IF @atom_id IS NULL
    BEGIN
        RAISERROR('AtomEmbedding not found: %I64d', 16, 1, @atom_embedding_id);
        RETURN -1;
    END
    
    BEGIN TRY
        -- Check if semantic features already exist
        IF EXISTS (SELECT 1 FROM dbo.SemanticFeatures WHERE AtomEmbeddingId = @atom_embedding_id)
        BEGIN
            -- Update existing features
            UPDATE dbo.SemanticFeatures
            SET 
                ComputedAt = SYSUTCDATETIME(),
                -- Feature computation would go here
                -- This is a simplified implementation
                Sentiment = 0.0,
                Toxicity = 0.0,
                Formality = 0.5,
                Complexity = 0.5
            WHERE AtomEmbeddingId = @atom_embedding_id;
        END
        ELSE
        BEGIN
            -- Insert new semantic features
            INSERT INTO dbo.SemanticFeatures (
                AtomEmbeddingId,
                AtomId,
                Sentiment,
                Toxicity,
                Formality,
                Complexity,
                TopicBusiness,
                TopicTechnical,
                TopicScientific,
                TopicCreative,
                TemporalIndicator,
                ComputedAt
            )
            VALUES (
                @atom_embedding_id,
                @atom_id,
                0.0,  -- Neutral sentiment
                0.0,  -- Non-toxic
                0.5,  -- Medium formality
                0.5,  -- Medium complexity
                0.0,  -- Topic scores would be computed from text analysis
                0.0,
                0.0,
                0.0,
                0.0,  -- No temporal indicator
                SYSUTCDATETIME()
            );
        END
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, 1);
        RETURN -2;
    END CATCH
END
GO

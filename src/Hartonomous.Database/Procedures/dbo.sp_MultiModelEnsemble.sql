-- sp_MultiModelEnsemble: Blend results from multiple embedding models
-- Weighted voting with configurable model weights

CREATE OR ALTER PROCEDURE dbo.sp_MultiModelEnsemble
    @QueryVector1 VARBINARY(MAX), -- Model 1 embedding
    @QueryVector2 VARBINARY(MAX), -- Model 2 embedding
    @QueryVector3 VARBINARY(MAX), -- Model 3 embedding
    @Model1Id INT,
    @Model2Id INT,
    @Model3Id INT,
    @Model1Weight FLOAT = 0.4,
    @Model2Weight FLOAT = 0.35,
    @Model3Weight FLOAT = 0.25,
    @TopK INT = 10,
    @TenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        DECLARE @EnsembleResults TABLE (
            AtomId BIGINT,
            Model1Score FLOAT,
            Model2Score FLOAT,
            Model3Score FLOAT,
            EnsembleScore FLOAT
        );
        
        -- Get all unique atoms from all models
        DECLARE @AllAtoms TABLE (AtomId BIGINT PRIMARY KEY);
        
        INSERT INTO @AllAtoms
        SELECT DISTINCT ae.AtomId
        FROM dbo.AtomEmbeddings ae
        WHERE ae.TenantId = @TenantId
              AND ae.ModelId IN (@Model1Id, @Model2Id, @Model3Id);
        
        -- Score each atom with each model
        INSERT INTO @EnsembleResults (AtomId, Model1Score, Model2Score, Model3Score)
        SELECT 
            aa.AtomId,
            ISNULL(
                (SELECT 1.0 - VECTOR_DISTANCE('cosine', ae1.EmbeddingVector, @QueryVector1)
                 FROM dbo.AtomEmbeddings ae1
                 WHERE ae1.AtomId = aa.AtomId AND ae1.ModelId = @Model1Id),
                0.0
            ) AS Model1Score,
            ISNULL(
                (SELECT 1.0 - VECTOR_DISTANCE('cosine', ae2.EmbeddingVector, @QueryVector2)
                 FROM dbo.AtomEmbeddings ae2
                 WHERE ae2.AtomId = aa.AtomId AND ae2.ModelId = @Model2Id),
                0.0
            ) AS Model2Score,
            ISNULL(
                (SELECT 1.0 - VECTOR_DISTANCE('cosine', ae3.EmbeddingVector, @QueryVector3)
                 FROM dbo.AtomEmbeddings ae3
                 WHERE ae3.AtomId = aa.AtomId AND ae3.ModelId = @Model3Id),
                0.0
            ) AS Model3Score
        FROM @AllAtoms aa;
        
        -- Compute weighted ensemble score
        UPDATE @EnsembleResults
        SET EnsembleScore = 
            (Model1Score * @Model1Weight) + 
            (Model2Score * @Model2Weight) + 
            (Model3Score * @Model3Weight);
        
        -- Return top K
        SELECT TOP (@TopK)
            er.AtomId,
            er.Model1Score,
            er.Model2Score,
            er.Model3Score,
            er.EnsembleScore,
            a.ContentHash,
            a.ContentType
        FROM @EnsembleResults er
        INNER JOIN dbo.Atoms a ON er.AtomId = a.AtomId
        WHERE a.TenantId = @TenantId
        ORDER BY er.EnsembleScore DESC;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;

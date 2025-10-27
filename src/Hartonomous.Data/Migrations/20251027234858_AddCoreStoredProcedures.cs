using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hartonomous.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCoreStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ==========================================
            // CORE INFERENCE PROCEDURES
            // From sql/procedures/06_ProductionSystem.sql
            // ==========================================

            // sp_ExactVectorSearch: Full precision VECTOR_DISTANCE search
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE dbo.sp_ExactVectorSearch
                    @query_vector VECTOR(768),
                    @top_k INT = 10,
                    @distance_metric NVARCHAR(20) = 'cosine'
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT TOP (@top_k)
                        EmbeddingId as embedding_id,
                        SourceText as source_text,
                        SourceType as source_type,
                        VECTOR_DISTANCE(@distance_metric, embedding_full, @query_vector) as distance,
                        1.0 - VECTOR_DISTANCE(@distance_metric, embedding_full, @query_vector) as similarity
                    FROM dbo.Embeddings_Production
                    WHERE embedding_full IS NOT NULL
                    ORDER BY VECTOR_DISTANCE(@distance_metric, embedding_full, @query_vector);
                END;
            ");

            // sp_HybridSearch: Spatial filter + Vector rerank
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE dbo.sp_HybridSearch
                    @query_vector VECTOR(768),
                    @query_spatial_x FLOAT,
                    @query_spatial_y FLOAT,
                    @query_spatial_z FLOAT,
                    @spatial_candidates INT = 100,
                    @final_top_k INT = 10
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- Fast spatial filter candidates
                    DECLARE @candidates TABLE (embedding_id BIGINT);

                    DECLARE @query_point GEOMETRY = geometry::STGeomFromText(
                        'POINT(' + CAST(@query_spatial_x AS NVARCHAR(50)) + ' ' +
                                   CAST(@query_spatial_y AS NVARCHAR(50)) + ' ' +
                                   CAST(@query_spatial_z AS NVARCHAR(50)) + ')', 0);

                    -- Use spatial indexes when available (production table has them, EF table uses X,Y,Z columns)
                    INSERT INTO @candidates
                    SELECT TOP (@spatial_candidates) EmbeddingId
                    FROM dbo.Embeddings_Production
                    WHERE SpatialProjX IS NOT NULL
                    ORDER BY 
                        SQRT(POWER(SpatialProjX - @query_spatial_x, 2) + 
                             POWER(SpatialProjY - @query_spatial_y, 2) + 
                             POWER(SpatialProjZ - @query_spatial_z, 2));

                    -- Exact vector rerank
                    SELECT TOP (@final_top_k)
                        ep.EmbeddingId as embedding_id,
                        ep.SourceText as source_text,
                        ep.SourceType as source_type,
                        VECTOR_DISTANCE('cosine', ep.embedding_full, @query_vector) as distance,
                        1.0 - VECTOR_DISTANCE('cosine', ep.embedding_full, @query_vector) as similarity,
                        SQRT(POWER(ep.SpatialProjX - @query_spatial_x, 2) + 
                             POWER(ep.SpatialProjY - @query_spatial_y, 2) + 
                             POWER(ep.SpatialProjZ - @query_spatial_z, 2)) as spatial_distance
                    FROM dbo.Embeddings_Production ep
                    JOIN @candidates c ON ep.EmbeddingId = c.embedding_id
                    ORDER BY VECTOR_DISTANCE('cosine', ep.embedding_full, @query_vector);
                END;
            ");

            // ==========================================
            // SPATIAL PROJECTION PROCEDURES
            // From sql/procedures/08_SpatialProjection.sql
            // ==========================================

            // sp_ComputeSpatialProjection: 768D -> 3D projection
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE dbo.sp_ComputeSpatialProjection
                    @input_vector VECTOR(768),
                    @output_x FLOAT OUTPUT,
                    @output_y FLOAT OUTPUT,
                    @output_z FLOAT OUTPUT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- Simple projection: Use first 3 PCA-like components
                    -- In production, this would use anchor-based distance calculation
                    -- For now, extract magnitude in 3 orthogonal subspaces

                    DECLARE @dim_per_proj INT = 256; -- 768 / 3

                    -- X: Sum of first 256 dimensions
                    DECLARE @idx INT = 0;
                    DECLARE @sum_x FLOAT = 0.0;
                    WHILE @idx < @dim_per_proj
                    BEGIN
                        SET @sum_x = @sum_x + CAST(JSON_VALUE(CAST(@input_vector AS NVARCHAR(MAX)), '$[' + CAST(@idx AS NVARCHAR) + ']') AS FLOAT);
                        SET @idx = @idx + 1;
                    END;
                    SET @output_x = @sum_x / @dim_per_proj;

                    -- Y: Sum of next 256 dimensions
                    DECLARE @sum_y FLOAT = 0.0;
                    WHILE @idx < (@dim_per_proj * 2)
                    BEGIN
                        SET @sum_y = @sum_y + CAST(JSON_VALUE(CAST(@input_vector AS NVARCHAR(MAX)), '$[' + CAST(@idx AS NVARCHAR) + ']') AS FLOAT);
                        SET @idx = @idx + 1;
                    END;
                    SET @output_y = @sum_y / @dim_per_proj;

                    -- Z: Sum of last 256 dimensions
                    DECLARE @sum_z FLOAT = 0.0;
                    WHILE @idx < 768
                    BEGIN
                        SET @sum_z = @sum_z + CAST(JSON_VALUE(CAST(@input_vector AS NVARCHAR(MAX)), '$[' + CAST(@idx AS NVARCHAR) + ']') AS FLOAT);
                        SET @idx = @idx + 1;
                    END;
                    SET @output_z = @sum_z / @dim_per_proj;
                END;
            ");

            // ==========================================
            // MODEL WEIGHT QUERY PROCEDURE
            // From sql/procedures/06_ProductionSystem.sql
            // ==========================================

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE dbo.sp_QueryModelWeights
                    @model_id INT,
                    @layer_name NVARCHAR(200) = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @layer_name IS NULL
                    BEGIN
                        -- Return all layers for this model
                        SELECT
                            LayerId,
                            LayerIdx,
                            LayerName,
                            LayerType,
                            ParameterCount,
                            QuantizationType,
                            CacheHitRate
                        FROM dbo.ModelLayers
                        WHERE ModelId = @model_id
                        ORDER BY LayerIdx;
                    END
                    ELSE
                    BEGIN
                        -- Return specific layer with weights
                        SELECT
                            LayerId,
                            LayerName,
                            LayerType,
                            Weights,
                            Parameters,
                            ParameterCount,
                            QuantizationType,
                            QuantizationScale,
                            QuantizationZeroPoint
                        FROM dbo.ModelLayers
                        WHERE ModelId = @model_id AND LayerName = @layer_name;
                    END;
                END;
            ");

            // ==========================================
            // FEEDBACK LOOP PROCEDURE
            // From sql/procedures/17_FeedbackLoop.sql
            // ==========================================

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE dbo.sp_UpdateModelWeightsFromFeedback
                    @model_id INT,
                    @min_rating TINYINT = 4,
                    @update_magnitude FLOAT = 0.01
                AS
                BEGIN
                    SET NOCOUNT ON;

                    PRINT 'Analyzing feedback for model ' + CAST(@model_id AS NVARCHAR(10));

                    -- Find highly-rated inferences for this model
                    DECLARE @good_inferences TABLE (
                        InferenceId BIGINT,
                        UserRating TINYINT,
                        LayerId BIGINT
                    );

                    INSERT INTO @good_inferences
                    SELECT
                        ir.InferenceId,
                        ir.UserRating,
                        ist.LayerId
                    FROM dbo.InferenceRequests ir
                    JOIN dbo.InferenceSteps ist ON ir.InferenceId = ist.InferenceId
                    WHERE ir.ModelId = @model_id
                      AND ir.UserRating >= @min_rating;

                    DECLARE @update_count INT = @@ROWCOUNT;
                    PRINT 'Found ' + CAST(@update_count AS NVARCHAR(10)) + ' highly-rated inferences';

                    -- Log update intent (actual weight update would require VECTOR_ADD/VECTOR_SCALE)
                    IF @update_count > 0
                    BEGIN
                        PRINT 'Would update weights with magnitude ' + CAST(@update_magnitude AS NVARCHAR(10));
                        
                        SELECT
                            ml.LayerName,
                            COUNT(*) as FeedbackCount,
                            AVG(CAST(gi.UserRating AS FLOAT)) as AvgRating
                        FROM @good_inferences gi
                        JOIN dbo.ModelLayers ml ON gi.LayerId = ml.LayerId
                        GROUP BY ml.LayerName
                        ORDER BY FeedbackCount DESC;
                    END
                    ELSE
                    BEGIN
                        PRINT 'No feedback available for weight updates';
                    END;
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop all stored procedures
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_ExactVectorSearch;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_HybridSearch;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_ComputeSpatialProjection;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_QueryModelWeights;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_UpdateModelWeightsFromFeedback;");
        }
    }
}

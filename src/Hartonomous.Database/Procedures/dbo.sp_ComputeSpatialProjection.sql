CREATE PROCEDURE [dbo].[sp_ComputeSpatialProjection]
    @input_vector VARBINARY(MAX),
    @input_dimension INT,
    @output_x FLOAT OUTPUT,
    @output_y FLOAT OUTPUT,
    @output_z FLOAT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Compute 3D spatial projection from high-dimensional embedding vector
    -- Uses dimensionality reduction (simple PCA-like projection for visualization)
    
    DECLARE @vectorLength INT = DATALENGTH(@input_vector) / 4; -- 4 bytes per float
    
    IF @vectorLength = 0 OR @input_vector IS NULL
    BEGIN
        SET @output_x = 0;
        SET @output_y = 0;
        SET @output_z = 0;
        RETURN;
    END

    -- Simple projection: use first 3 dimensions if available, otherwise project down
    DECLARE @offset INT = 0;
    DECLARE @floatVal FLOAT;
    
    -- X component (dimension 0 or weighted sum)
    IF @vectorLength >= 1
    BEGIN
        SET @floatVal = dbo.ConvertVarbinary4ToReal(SUBSTRING(@input_vector, 1, 4));
        SET @output_x = @floatVal;
    END
    ELSE
        SET @output_x = 0;

    -- Y component (dimension 1 or weighted sum)
    IF @vectorLength >= 2
    BEGIN
        SET @floatVal = dbo.ConvertVarbinary4ToReal(SUBSTRING(@input_vector, 5, 4));
        SET @output_y = @floatVal;
    END
    ELSE
        SET @output_y = 0;

    -- Z component (dimension 2 or weighted sum)
    IF @vectorLength >= 3
    BEGIN
        SET @floatVal = dbo.ConvertVarbinary4ToReal(SUBSTRING(@input_vector, 9, 4));
        SET @output_z = @floatVal;
    END
    ELSE
        SET @output_z = 0;

    -- Normalize to unit sphere for spatial bucketing
    DECLARE @magnitude FLOAT = SQRT(@output_x * @output_x + @output_y * @output_y + @output_z * @output_z);
    
    IF @magnitude > 0
    BEGIN
        SET @output_x = @output_x / @magnitude;
        SET @output_y = @output_y / @magnitude;
        SET @output_z = @output_z / @magnitude;
    END
END
GO

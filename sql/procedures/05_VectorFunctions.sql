-- =============================================
-- Vector Functions
-- =============================================

USE Hartonomous;
GO

CREATE OR ALTER FUNCTION dbo.VectorDotProduct (@v1 VARBINARY(MAX), @v2 VARBINARY(MAX))
RETURNS FLOAT
AS
BEGIN
    RETURN dbo.clr_VectorDotProduct(@v1, @v2);
END
GO

CREATE OR ALTER FUNCTION dbo.VectorCosineSimilarity (@v1 VARBINARY(MAX), @v2 VARBINARY(MAX))
RETURNS FLOAT
AS
BEGIN
    RETURN dbo.clr_VectorCosineSimilarity(@v1, @v2);
END
GO

CREATE OR ALTER FUNCTION dbo.VectorEuclideanDistance (@v1 VARBINARY(MAX), @v2 VARBINARY(MAX))
RETURNS FLOAT
AS
BEGIN
    RETURN dbo.clr_VectorEuclideanDistance(@v1, @v2);
END
GO

CREATE OR ALTER FUNCTION dbo.VectorNormalize (@v VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS
BEGIN
    RETURN dbo.clr_VectorNormalize(@v);
END
GO

CREATE OR ALTER FUNCTION dbo.VectorSoftmax (@v VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS
BEGIN
    RETURN dbo.clr_VectorSoftmax(@v);
END
GO

CREATE OR ALTER FUNCTION dbo.VectorArgMax (@v VARBINARY(MAX))
RETURNS INT
AS
BEGIN
    RETURN dbo.clr_VectorArgMax(@v);
END
GO

CREATE OR ALTER FUNCTION dbo.VectorAdd (@v1 VARBINARY(MAX), @v2 VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS
BEGIN
    RETURN dbo.clr_VectorAdd(@v1, @v2);
END
GO

CREATE OR ALTER FUNCTION dbo.VectorSubtract (@v1 VARBINARY(MAX), @v2 VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS
BEGIN
    RETURN dbo.clr_VectorSubtract(@v1, @v2);
END
GO

CREATE OR ALTER FUNCTION dbo.VectorScale (@v VARBINARY(MAX), @scalar FLOAT)
RETURNS VARBINARY(MAX)
AS
BEGIN
    RETURN dbo.clr_VectorScale(@v, @scalar);
END
GO

CREATE OR ALTER FUNCTION dbo.VectorLerp (@v1 VARBINARY(MAX), @v2 VARBINARY(MAX), @t FLOAT)
RETURNS VARBINARY(MAX)
AS
BEGIN
    RETURN dbo.clr_VectorLerp(@v1, @v2, @t);
END
GO

CREATE OR ALTER FUNCTION dbo.VectorNorm (@v VARBINARY(MAX))
RETURNS FLOAT
AS
BEGIN
    RETURN dbo.clr_VectorNorm(@v);
END
GO

-- ==================================================================
-- ref.GetStatusId: Helper function for status code to ID lookup
-- ==================================================================
CREATE FUNCTION [ref].[GetStatusId](@StatusCode VARCHAR(50))
RETURNS INT
WITH SCHEMABINDING
AS
BEGIN
    RETURN (
        SELECT StatusId 
        FROM [ref].[Status] 
        WHERE Code = UPPER(@StatusCode) 
          AND IsActive = 1
    );
END;
GO

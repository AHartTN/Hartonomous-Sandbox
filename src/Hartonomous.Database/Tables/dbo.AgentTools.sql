CREATE TABLE dbo.AgentTools (
    ToolId INT IDENTITY(1,1) PRIMARY KEY,
    ToolName NVARCHAR(128) NOT NULL UNIQUE,
    Description NVARCHAR(1024) NOT NULL,
    ObjectType NVARCHAR(128) NOT NULL, -- e.g., 'STORED_PROCEDURE', 'SCALAR_FUNCTION', 'TABLE_VALUED_FUNCTION'
    ObjectName NVARCHAR(256) NOT NULL,  -- e.g., 'dbo.sp_SomeTool'
    ParametersJson NVARCHAR(MAX),      -- A JSON schema describing the parameters
    IsEnabled BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
);

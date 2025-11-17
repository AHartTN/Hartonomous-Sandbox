CREATE TABLE dbo.AgentTools (
    ToolId BIGINT IDENTITY(1,1) PRIMARY KEY,
    ToolName NVARCHAR(200) NOT NULL UNIQUE,
    ToolCategory NVARCHAR(100),
    Description NVARCHAR(2000),
    ObjectType NVARCHAR(128) NOT NULL, -- e.g., 'STORED_PROCEDURE', 'SCALAR_FUNCTION', 'TABLE_VALUED_FUNCTION'
    ObjectName NVARCHAR(256) NOT NULL,  -- e.g., 'dbo.sp_SomeTool'
    ParametersJson JSON,      -- Native JSON schema describing tool parameters
    IsEnabled BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
);
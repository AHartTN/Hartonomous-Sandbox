USE [$(DatabaseName)]
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AgentTools WHERE ToolName = 'analyze_system_state')
BEGIN
    INSERT INTO dbo.AgentTools (ToolName, Description, ObjectType, ObjectName, ParametersJson)
    VALUES (
        'analyze_system_state',
        'Performs a comprehensive analysis of the database system''s current state. Use this tool to answer questions about system performance, slow queries, test failures, or cost hotspots. It returns a JSON document containing detailed metrics.',
        'TABLE_VALUED_FUNCTION',
        'dbo.fn_clr_AnalyzeSystemState',
        '{"type":"object","properties":{"target_area":{"type":"string","description":"Optional. The specific area to focus on, e.g., ''performance'', ''cost'', or ''testing''."}}}'
    );
END
GO
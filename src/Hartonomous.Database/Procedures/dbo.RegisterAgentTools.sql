-- =============================================
-- Agent Tool Registration
-- =============================================
-- This script registers the initial set of tools
-- for the autonomous agent framework.
-- =============================================

USE Hartonomous;
GO

PRINT 'Registering agent tools...';

-- Clear existing tools to ensure idempotency
-- In a production system, you might use MERGE instead.
TRUNCATE TABLE dbo.AgentTools;
GO

-- 1. System Analysis Tool
-------------------------------------------------
-- Registers the CLR function that performs a comprehensive
-- analysis of the database system's health and performance.
-------------------------------------------------
INSERT INTO dbo.AgentTools (ToolName, Description, ObjectType, ObjectName, ParametersJson)
VALUES (
    'analyze_system_state',
    'Performs a comprehensive analysis of the database system''s current state. Use this tool to answer questions about system performance, slow queries, test failures, or cost hotspots. It returns a JSON document containing detailed metrics.',
    'TABLE_VALUED_FUNCTION',
    'dbo.fn_clr_AnalyzeSystemState',
    '{"type":"object","properties":{"target_area":{"type":"string","description":"Optional. The specific area to focus on, e.g., ''performance'', ''cost'', or ''testing''."}}}'
);
GO

PRINT 'Registered tool: analyze_system_state';
GO

-- Add more tools here in the future...

PRINT 'Agent tool registration complete.';
GO

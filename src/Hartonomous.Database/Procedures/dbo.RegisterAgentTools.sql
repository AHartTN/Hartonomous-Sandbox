-- =============================================
-- Agent Tool Registration
-- =============================================
-- This script registers the initial set of tools
-- for the autonomous agent framework.
-- =============================================

-- Clear existing tools to ensure idempotency
-- In a production system, you might use MERGE instead.
TRUNCATE TABLE dbo.AgentTools;

-- 1. System Analysis Tool
-------------------------------------------------
-- Registers the CLR function that performs a comprehensive
-- analysis of the database system's health and performance.
-------------------------------------------------

-- Add more tools here in the future...

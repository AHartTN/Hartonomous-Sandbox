-- =============================================
-- Hartonomous Local Development Seed Data
-- =============================================
-- Inserts test data for local development and testing.
-- Safe to run multiple times (uses MERGE for upserts).
--
-- Includes:
-- 1. Billing rate plans (Publisher Core, Publisher Pro, Publisher Enterprise)
-- 2. Billing operation rates (ingestion, generation, inference)
-- 3. Billing multipliers (content type, grounding, guarantees, provenance)
-- 4. Sample model metadata (for testing)
-- =============================================

SET NOCOUNT ON;
GO

PRINT '=== Seeding Local Development Data ===';
GO

-- =============================================
-- 1. BILLING RATE PLANS
-- =============================================
PRINT 'Seeding billing rate plans...';

MERGE INTO dbo.BillingRatePlans AS target
USING (VALUES
    ('publisher_core', 'Publisher Core', 2500.00, 0.0105, 0.00008, 120, 40, 25, 1, 1),
    ('publisher_pro', 'Publisher Pro', 5000.00, 0.0095, 0.000075, 250, 100, 50, 1, 1),
    ('publisher_enterprise', 'Publisher Enterprise', 10000.00, 0.0085, 0.00007, 500, 250, 100, 1, 1)
) AS source (PlanCode, PlanName, MonthlyFee, DefaultRate, UnitPricePerDcu, IncludedPublicStorageGb, IncludedPrivateStorageGb, IncludedSeatCount, AllowsPrivateData, CanQueryPublicCorpus)
ON target.PlanCode = source.PlanCode
WHEN MATCHED THEN
    UPDATE SET
        PlanName = source.PlanName,
        MonthlyFee = source.MonthlyFee,
        DefaultRate = source.DefaultRate,
        UnitPricePerDcu = source.UnitPricePerDcu,
        IncludedPublicStorageGb = source.IncludedPublicStorageGb,
        IncludedPrivateStorageGb = source.IncludedPrivateStorageGb,
        IncludedSeatCount = source.IncludedSeatCount,
        AllowsPrivateData = source.AllowsPrivateData,
        CanQueryPublicCorpus = source.CanQueryPublicCorpus
WHEN NOT MATCHED THEN
    INSERT (PlanCode, PlanName, MonthlyFee, DefaultRate, UnitPricePerDcu, IncludedPublicStorageGb, IncludedPrivateStorageGb, IncludedSeatCount, AllowsPrivateData, CanQueryPublicCorpus)
    VALUES (source.PlanCode, source.PlanName, source.MonthlyFee, source.DefaultRate, source.UnitPricePerDcu, source.IncludedPublicStorageGb, source.IncludedPrivateStorageGb, source.IncludedSeatCount, source.AllowsPrivateData, source.CanQueryPublicCorpus);

PRINT '  ✓ ' + CAST(@@ROWCOUNT AS VARCHAR) + ' billing rate plans upserted';
GO

-- =============================================
-- 2. BILLING OPERATION RATES
-- =============================================
PRINT 'Seeding billing operation rates...';

MERGE INTO dbo.BillingOperationRates AS target
USING (VALUES
    ('ingest.atom', 0.018, 'dcu', 'ingestion'),
    ('ingest.embedding', 0.022, 'dcu', 'ingestion'),
    ('ingest.model', 0.035, 'dcu', 'model_management'),
    ('generation.text', 0.040, 'dcu', 'generation'),
    ('generation.image', 0.120, 'dcu', 'generation'),
    ('generation.audio', 0.088, 'dcu', 'generation'),
    ('generation.video', 0.152, 'dcu', 'generation'),
    ('inference.query', 0.025, 'dcu', 'inference'),
    ('inference.vector_search', 0.015, 'dcu', 'inference'),
    ('inference.semantic_search', 0.030, 'dcu', 'inference'),
    ('model.training', 0.100, 'dcu', 'model_management'),
    ('model.fine_tuning', 0.150, 'dcu', 'model_management'),
    ('neo4j_sync.model_updated', 0.025, 'dcu', 'model_management'),
    ('neo4j_sync.inference_completed', 0.040, 'dcu', 'generation'),
    ('neo4j_sync.ingest_completed', 0.018, 'dcu', 'ingestion')
) AS source (OperationName, Rate, UnitName, Category)
ON target.OperationName = source.OperationName
WHEN MATCHED THEN
    UPDATE SET
        Rate = source.Rate,
        UnitName = source.UnitName,
        Category = source.Category
WHEN NOT MATCHED THEN
    INSERT (OperationName, Rate, UnitName, Category)
    VALUES (source.OperationName, source.Rate, source.UnitName, source.Category);

PRINT '  ✓ ' + CAST(@@ROWCOUNT AS VARCHAR) + ' operation rates upserted';
GO

-- =============================================
-- 3. BILLING MULTIPLIERS
-- =============================================
PRINT 'Seeding billing multipliers...';

MERGE INTO dbo.BillingMultipliers AS target
USING (VALUES
    -- Generation Type Multipliers
    ('generation_type', 'text', 1.0),
    ('generation_type', 'image', 3.0),
    ('generation_type', 'audio', 2.2),
    ('generation_type', 'video', 3.8),

    -- Complexity Multipliers
    ('complexity', 'standard', 1.0),
    ('complexity', 'premium', 1.5),
    ('complexity', 'enterprise', 2.0),

    -- Content Type Multipliers
    ('content_type', 'knowledge_graph', 1.2),
    ('content_type', 'time_series', 1.4),
    ('content_type', 'spatial', 1.6),

    -- Grounding Multipliers
    ('grounding', 'none', 1.0),
    ('grounding', 'enterprise_context', 1.3),
    ('grounding', 'private_vector_index', 1.55),

    -- Guarantee Multipliers
    ('guarantee', 'standard_sla', 1.0),
    ('guarantee', 'premium_sla', 1.35),
    ('guarantee', 'model_lock', 1.2),

    -- Provenance Multipliers
    ('provenance', 'basic', 1.0),
    ('provenance', 'audit_trail', 1.25),
    ('provenance', 'immutable_ledger', 1.5)
) AS source (Category, Name, Multiplier)
ON target.Category = source.Category AND target.Name = source.Name
WHEN MATCHED THEN
    UPDATE SET Multiplier = source.Multiplier
WHEN NOT MATCHED THEN
    INSERT (Category, Name, Multiplier)
    VALUES (source.Category, source.Name, source.Multiplier);

PRINT '  ✓ ' + CAST(@@ROWCOUNT AS VARCHAR) + ' billing multipliers upserted';
GO

-- =============================================
-- 4. SAMPLE MODELS (for testing)
-- =============================================
PRINT 'Seeding sample models...';

DECLARE @TestModelId UNIQUEIDENTIFIER = NEWID();

MERGE INTO dbo.Models AS target
USING (VALUES
    (@TestModelId, 'test-model-llama-3.1-8b', 'Test Model - Llama 3.1 8B', 'test', 'llama', '3.1', 8000000000, 4096, 'bfloat16', GETUTCDATE())
) AS source (ModelId, ModelName, ModelDescription, ModelSource, Architecture, Version, ParameterCount, ContextLength, QuantizationFormat, CreatedAt)
ON target.ModelName = source.ModelName
WHEN MATCHED THEN
    UPDATE SET
        ModelDescription = source.ModelDescription,
        ModelSource = source.ModelSource,
        Architecture = source.Architecture,
        Version = source.Version,
        ParameterCount = source.ParameterCount,
        ContextLength = source.ContextLength,
        QuantizationFormat = source.QuantizationFormat
WHEN NOT MATCHED THEN
    INSERT (ModelId, ModelName, ModelDescription, ModelSource, Architecture, Version, ParameterCount, ContextLength, QuantizationFormat, CreatedAt)
    VALUES (source.ModelId, source.ModelName, source.ModelDescription, source.ModelSource, source.Architecture, source.Version, source.ParameterCount, source.ContextLength, source.QuantizationFormat, source.CreatedAt);

PRINT '  ✓ ' + CAST(@@ROWCOUNT AS VARCHAR) + ' test models upserted';
GO

-- =============================================
-- VERIFICATION QUERIES
-- =============================================
PRINT '';
PRINT '=== Seed Data Summary ===';

SELECT 'Billing Rate Plans' AS TableName, COUNT(*) AS RowCount FROM dbo.BillingRatePlans
UNION ALL
SELECT 'Billing Operation Rates', COUNT(*) FROM dbo.BillingOperationRates
UNION ALL
SELECT 'Billing Multipliers', COUNT(*) FROM dbo.BillingMultipliers
UNION ALL
SELECT 'Models', COUNT(*) FROM dbo.Models;

PRINT '';
PRINT '✓ Local development seed data deployment complete';
GO

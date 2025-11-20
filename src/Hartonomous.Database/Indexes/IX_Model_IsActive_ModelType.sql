CREATE NONCLUSTERED INDEX IX_Model_IsActive_ModelType
ON dbo.Model(IsActive, ModelType, ModelName)
INCLUDE (ModelId, Architecture, ParameterCount, TenantId)
WHERE IsActive = 1;

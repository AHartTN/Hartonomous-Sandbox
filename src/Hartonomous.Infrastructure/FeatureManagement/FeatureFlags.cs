namespace Hartonomous.Infrastructure.FeatureManagement;

/// <summary>
/// Feature flag names for Hartonomous system
/// </summary>
public static class FeatureFlags
{
    // Beta features
    public const string OodaLoop = "OodaLoop";
    public const string MultiModalInference = "MultiModalInference";
    public const string AdvancedCaching = "AdvancedCaching";
    
    // Experimental algorithms
    public const string GraphOptimization = "GraphOptimization";
    public const string AutoScaling = "AutoScaling";
    public const string PredictivePreloading = "PredictivePreloading";
    
    // A/B testing
    public const string NewSearchAlgorithm = "NewSearchAlgorithm";
    public const string ImprovedEmbeddings = "ImprovedEmbeddings";
    
    // Gradual rollout
    public const string VideoGeneration = "VideoGeneration";
    public const string AudioGeneration = "AudioGeneration";
    public const string SpatialIndexing = "SpatialIndexing";
    
    // Tenant-specific features
    public const string EnterpriseFeatures = "EnterpriseFeatures";
    public const string AdvancedAnalytics = "AdvancedAnalytics";
    public const string CustomModels = "CustomModels";
}

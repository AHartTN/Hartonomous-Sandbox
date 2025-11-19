namespace Hartonomous.Clr.Enums
{
    /// <summary>
    /// OODA loop hypothesis types with int backing for SQL Server CLR performance.
    /// Used by sp_Hypothesize for autonomous system improvement.
    /// </summary>
    public enum HypothesisType : int
    {
        Unknown = 0,
        IndexOptimization = 1,      // Create/rebuild spatial indexes
        QueryRegression = 2,        // Fix degraded query performance
        CacheWarming = 3,           // Preload frequently accessed data
        ConceptDiscovery = 4,       // Extract new concept embeddings
        PruneModel = 5,             // Remove low-importance weights
        RefactorCode = 6,           // Suggest code improvements
        FixUX = 7                   // Improve user experience patterns
    }
}

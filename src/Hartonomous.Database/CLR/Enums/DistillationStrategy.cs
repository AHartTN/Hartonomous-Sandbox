namespace Hartonomous.Clr.Enums
{
    /// <summary>
    /// Model distillation strategies with int backing for SQL Server CLR performance.
    /// Methods for compressing large models into smaller, faster variants.
    /// </summary>
    public enum DistillationStrategy : int
    {
        None = 0,
        KnowledgeDistillation = 1,  // Teacher-student training
        LayerWisePruning = 2,       // Remove entire layers
        AttentionHeadPruning = 3,   // Remove attention heads
        WeightQuantization = 4,     // Reduce precision
        StructuredPruning = 5,      // Remove neurons/filters
        UnstructuredPruning = 6     // Remove individual weights
    }
}

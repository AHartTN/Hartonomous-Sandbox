namespace Hartonomous.Clr.Enums
{
    /// <summary>
    /// Model pruning strategies with int backing for SQL Server CLR performance.
    /// Criteria for removing model weights during OODA loop optimization.
    /// </summary>
    public enum PruningStrategy : int
    {
        None = 0,
        MagnitudeBased = 1,     // Remove smallest weights
        GradientBased = 2,      // Remove low-gradient weights
        ImportanceBased = 3,    // Remove based on loss impact
        ActivationBased = 4,    // Remove rarely activated neurons
        Lottery = 5,            // Lottery ticket hypothesis
        SNIP = 6                // Single-shot pruning at init
    }
}

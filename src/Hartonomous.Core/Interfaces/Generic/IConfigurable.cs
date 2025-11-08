namespace Hartonomous.Core.Interfaces.Generic;

public interface IConfigurable<TConfig> where TConfig : class
{
    /// <summary>
    /// Configure the service with the provided configuration.
    /// </summary>
    /// <param name="config">The configuration to apply</param>
    void Configure(TConfig config);

    /// <summary>
    /// Get the current configuration.
    /// </summary>
    /// <returns>The current configuration</returns>
    TConfig GetConfiguration();

    /// <summary>
    /// Validate the configuration.
    /// </summary>
    /// <param name="config">The configuration to validate</param>
    /// <returns>Validation result</returns>
    ValidationResult ValidateConfiguration(TConfig config);
}

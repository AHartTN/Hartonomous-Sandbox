namespace Hartonomous.IntegrationTests;

/// <summary>
/// Test application factory with Azure services enabled.
/// NOTE: Azure services are now managed by environment-specific factories.
/// Use ProductionConfigWebApplicationFactory for Azure configuration testing.
/// This class is kept for backwards compatibility but does nothing special.
/// </summary>
public class AzureEnabledWebApplicationFactory : HartonomousWebApplicationFactory
{
    public AzureEnabledWebApplicationFactory() : base()
    {
    }
}

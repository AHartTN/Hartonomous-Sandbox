namespace Hartonomous.IntegrationTests;

/// <summary>
/// Test application factory with Azure services enabled.
/// Uses the HART-DESKTOP Arc managed identity for App Configuration, Key Vault, and Application Insights.
/// </summary>
public class AzureEnabledWebApplicationFactory : HartonomousWebApplicationFactory
{
    public AzureEnabledWebApplicationFactory() 
        : base(environment: "Development", enableAzureServices: true)
    {
    }
}

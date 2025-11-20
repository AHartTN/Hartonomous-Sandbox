using Hartonomous.Core.Configuration;
using Hartonomous.Infrastructure.Configurations;
using Hartonomous.Workers.Neo4jSync;
using Azure.Identity;

var builder = Host.CreateApplicationBuilder(args);

// ===== PHASE 5.2: Configure Neo4j Sync Worker =====

// Azure App Configuration (if enabled)
var appConfigEndpoint = builder.Configuration["AzureAppConfiguration:Endpoint"];
var appConfigEnabled = builder.Configuration.GetValue<bool>("AzureAppConfiguration:Enabled", true);
var useManagedIdentity = builder.Configuration.GetValue<bool>("Azure:UseManagedIdentity", true);

if (appConfigEnabled && !string.IsNullOrEmpty(appConfigEndpoint))
{
    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options.Connect(new Uri(appConfigEndpoint),
            useManagedIdentity
                ? new DefaultAzureCredential()
                : new DefaultAzureCredential(new DefaultAzureCredentialOptions { ExcludeManagedIdentityCredential = true }));

        options.Select("*", builder.Environment.EnvironmentName);
        options.Select("*", null);

        options.ConfigureRefresh(refresh =>
        {
            refresh.Register("Sentinel", refreshAll: true)
                   .SetRefreshInterval(TimeSpan.FromMinutes(5));
        });
    });
}

// Azure Key Vault (if enabled)
var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
var keyVaultEnabled = builder.Configuration.GetValue<bool>("KeyVault:Enabled", true);

if (keyVaultEnabled && !string.IsNullOrEmpty(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        useManagedIdentity
            ? new DefaultAzureCredential()
            : new DefaultAzureCredential(new DefaultAzureCredentialOptions { ExcludeManagedIdentityCredential = true }));
}

// Configure strongly-typed options
builder.Services.AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<Neo4jOptions>()
    .Bind(builder.Configuration.GetSection(Neo4jOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<ApplicationInsightsOptions>()
    .Bind(builder.Configuration.GetSection(ApplicationInsightsOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Application Insights
var appInsightsConnection = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrEmpty(appInsightsConnection))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = appInsightsConnection;
    });
}

// Register business services (includes Neo4j provenance service)
builder.Services.AddBusinessServices();

// Register the Neo4j Sync worker
builder.Services.AddHostedService<Neo4jSyncWorker>();

var host = builder.Build();
host.Run();

using Hartonomous.Core.Configuration;
using Hartonomous.Infrastructure.Configurations;
using Hartonomous.Workers.CesConsumer;
using Azure.Identity;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

// ===== PHASE 5.1: Configure CES Consumer Worker =====

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

// Register business services (DbContext, IIngestionService)
builder.Services.AddBusinessServices();

// Register minimal ingestion dependencies (atomizers, file detection)
// Note: Do NOT call AddIngestionServices() as it registers StreamingIngestionService (requires SignalR)
builder.Services.AddSingleton<Hartonomous.Core.Interfaces.Ingestion.IFileTypeDetector, Hartonomous.Infrastructure.FileType.FileTypeDetector>();
builder.Services.AddTransient<Hartonomous.Core.Interfaces.Ingestion.IAtomizer<byte[]>, Hartonomous.Infrastructure.Atomizers.TextAtomizer>();
builder.Services.AddTransient<Hartonomous.Core.Interfaces.Ingestion.IAtomizer<byte[]>, Hartonomous.Infrastructure.Atomizers.ImageAtomizer>();
builder.Services.AddTransient<Hartonomous.Core.Interfaces.Ingestion.IAtomizer<byte[]>, Hartonomous.Infrastructure.Atomizers.DocumentAtomizer>();
builder.Services.AddTransient<Hartonomous.Core.Interfaces.Ingestion.IAtomizer<byte[]>, Hartonomous.Infrastructure.Atomizers.VideoFileAtomizer>();
builder.Services.AddTransient<Hartonomous.Core.Interfaces.Ingestion.IAtomizer<byte[]>, Hartonomous.Infrastructure.Atomizers.AudioFileAtomizer>();
builder.Services.AddTransient<Hartonomous.Core.Interfaces.Ingestion.IAtomizer<byte[]>, Hartonomous.Infrastructure.Atomizers.CodeFileAtomizer>();
builder.Services.AddTransient<Hartonomous.Core.Interfaces.Ingestion.IAtomizer<byte[]>, Hartonomous.Infrastructure.Atomizers.ArchiveAtomizer>();
builder.Services.AddTransient<Hartonomous.Core.Interfaces.Ingestion.IAtomizer<byte[]>, Hartonomous.Infrastructure.Atomizers.ModelFileAtomizer>();

// Bulk insert service for atomizers
builder.Services.AddScoped<Hartonomous.Core.Interfaces.Ingestion.IAtomBulkInsertService>(sp =>
{
    var databaseOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
    var logger = sp.GetRequiredService<ILogger<Hartonomous.Infrastructure.Services.AtomBulkInsertService>>();
    return new Hartonomous.Infrastructure.Services.AtomBulkInsertService(databaseOptions.HartonomousDb, logger);
});

// Register the CES Consumer worker
builder.Services.AddHostedService<CesConsumerWorker>();

var host = builder.Build();
host.Run();

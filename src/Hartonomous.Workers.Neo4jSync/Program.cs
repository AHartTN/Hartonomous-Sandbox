using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Billing;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Messaging;
using Hartonomous.Core.Serialization;
using Hartonomous.Core.Resilience;
using Hartonomous.Core.Security;
using Hartonomous.Infrastructure.Data;
using Hartonomous.Infrastructure.Extensions;
using Hartonomous.Infrastructure.Services.Billing;
using Hartonomous.Infrastructure.Services.Messaging;
using Hartonomous.Infrastructure.Services.Security;
using Hartonomous.Workers.Neo4jSync.Services;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

// Azure App Configuration integration (production only)
var appConfigEndpoint = builder.Configuration["Endpoints:AppConfiguration"];
if (!string.IsNullOrEmpty(appConfigEndpoint)
    && builder.Environment.IsProduction())
{
    // Production: Use Azure Arc managed identity
    var credential = new DefaultAzureCredential();

    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options.Connect(new Uri(appConfigEndpoint), credential)
            // Configure Key Vault integration for secret references
            .ConfigureKeyVault(kv =>
            {
                kv.SetCredential(credential);
            });
    });
}

var sqlConnectionString = Environment.GetEnvironmentVariable("HARTONOMOUS_SQL_CONNECTION")
    ?? builder.Configuration.GetConnectionString("HartonomousDb")
    ?? "Server=localhost;Database=Hartonomous;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.Configure<SqlServerOptions>(builder.Configuration.GetSection(SqlServerOptions.SectionName));
builder.Services.PostConfigure<SqlServerOptions>(options =>
{
    options.ConnectionString = string.IsNullOrWhiteSpace(options.ConnectionString)
        ? sqlConnectionString
        : options.ConnectionString;

    if (options.CommandTimeoutSeconds <= 0)
    {
        options.CommandTimeoutSeconds = 30;
    }
});

builder.Services.Configure<MessageBrokerOptions>(builder.Configuration.GetSection(MessageBrokerOptions.SectionName));
builder.Services.PostConfigure<MessageBrokerOptions>(options =>
{
    if (options.ReceiveWaitTimeoutMilliseconds < 250)
    {
        options.ReceiveWaitTimeoutMilliseconds = 250;
    }
});

builder.Services.Configure<ServiceBrokerResilienceOptions>(builder.Configuration.GetSection(ServiceBrokerResilienceOptions.SectionName));
builder.Services.PostConfigure<ServiceBrokerResilienceOptions>(options =>
{
    if (options.PublishBaseDelay <= TimeSpan.Zero)
    {
        options.PublishBaseDelay = TimeSpan.FromMilliseconds(250);
    }

    if (options.ReceiveBaseDelay <= TimeSpan.Zero)
    {
        options.ReceiveBaseDelay = TimeSpan.FromMilliseconds(500);
    }

    if (options.PublishMaxDelay < options.PublishBaseDelay)
    {
        options.PublishMaxDelay = options.PublishBaseDelay;
    }

    if (options.ReceiveMaxDelay < options.ReceiveBaseDelay)
    {
        options.ReceiveMaxDelay = options.ReceiveBaseDelay;
    }
});

builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection(SecurityOptions.SectionName));
builder.Services.Configure<BillingOptions>(builder.Configuration.GetSection(BillingOptions.SectionName));
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<IJsonSerializer, SystemTextJsonSerializer>();
builder.Services.AddSingleton<ISqlServerConnectionFactory, SqlServerConnectionFactory>();
builder.Services.AddSingleton<IMessageBroker, SqlMessageBroker>();
builder.Services.AddSingleton<ITransientErrorDetector, SqlServerTransientErrorDetector>();
builder.Services.AddSingleton<Func<RetryPolicyOptions, IRetryPolicy>>(sp => options =>
{
    var detector = sp.GetRequiredService<ITransientErrorDetector>();
    var logger = sp.GetRequiredService<ILogger<ExponentialBackoffRetryPolicy>>();
    return new ExponentialBackoffRetryPolicy(options, detector, logger);
});
builder.Services.AddSingleton<ICircuitBreakerPolicy>(sp =>
{
    var resilienceOptions = sp.GetRequiredService<IOptions<ServiceBrokerResilienceOptions>>().Value;
    var options = new CircuitBreakerOptions
    {
        FailureThreshold = resilienceOptions.CircuitBreakerFailureThreshold,
        BreakDuration = resilienceOptions.CircuitBreakerBreakDuration,
        HalfOpenSuccessThreshold = resilienceOptions.CircuitBreakerHalfOpenSuccessThreshold
    };

    return new CircuitBreakerPolicy(options, sp.GetRequiredService<ITransientErrorDetector>(), sp.GetRequiredService<ILogger<CircuitBreakerPolicy>>());
});
builder.Services.AddSingleton<IServiceBrokerResilienceStrategy, ServiceBrokerResilienceStrategy>();
builder.Services.AddSingleton<IMessageDeadLetterSink, SqlMessageDeadLetterSink>();
builder.Services.AddSingleton<IAccessPolicyRule, TenantAccessPolicyRule>();
builder.Services.AddSingleton<IAccessPolicyEngine, AccessPolicyEngine>();
builder.Services.AddSingleton<IThrottleEvaluator, InMemoryThrottleEvaluator>();
builder.Services.AddSingleton<IBillingConfigurationProvider, SqlBillingConfigurationProvider>();
builder.Services.AddSingleton<IBillingMeter, UsageBillingMeter>();
builder.Services.AddSingleton<IBillingUsageSink, SqlBillingUsageSink>();
builder.Services.AddNeo4j(builder.Configuration);
builder.Services.AddSingleton<ProvenanceGraphBuilder>();
builder.Services.AddSingleton<IBaseEventHandler, ModelEventHandler>();
builder.Services.AddSingleton<IBaseEventHandler, InferenceEventHandler>();
builder.Services.AddSingleton<IBaseEventHandler, KnowledgeEventHandler>();
builder.Services.AddSingleton<IBaseEventHandler, GenericEventHandler>();
builder.Services.AddSingleton<IMessageDispatcher, EventDispatcher>();
builder.Services.AddHostedService<ServiceBrokerMessagePump>();

await builder.Build().RunAsync();

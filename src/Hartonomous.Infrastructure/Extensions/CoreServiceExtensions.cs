using System;
using Hartonomous.Core.Billing;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Messaging;
using Hartonomous.Core.Security;
using Hartonomous.Core.Serialization;
using Hartonomous.Core.Services;
using Hartonomous.Infrastructure.Caching;
using Hartonomous.Infrastructure.Compliance;
using Hartonomous.Infrastructure.Data;
using Hartonomous.Infrastructure.Services;
using Hartonomous.Infrastructure.Services.Billing;
using Hartonomous.Infrastructure.Services.Messaging;
using Hartonomous.Infrastructure.Services.Security;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hartonomous.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering core infrastructure services (serialization, messaging, caching, security, billing)
/// </summary>
public static class CoreServiceExtensions
{
    /// <summary>
    /// Registers core infrastructure services
    /// </summary>
    public static IServiceCollection AddHartonomousCoreServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration services
        services.Configure<MessageBrokerOptions>(configuration.GetSection(MessageBrokerOptions.SectionName));
        services.Configure<CdcOptions>(configuration.GetSection(CdcOptions.SectionName));
        services.Configure<BillingOptions>(configuration.GetSection(BillingOptions.SectionName));
        services.Configure<SecurityOptions>(configuration.GetSection(SecurityOptions.SectionName));
        services.Configure<AtomGraphOptions>(configuration.GetSection(AtomGraphOptions.SectionName));

        // Serialization
        services.AddSingleton<IJsonSerializer, SystemTextJsonSerializer>();

        // Database connection infrastructure
        services.AddSingleton<ISqlServerConnectionFactory, SqlServerConnectionFactory>();
        services.AddScoped<ISqlCommandExecutor, SqlCommandExecutor>();

        // Caching (memory + distributed)
        services.AddMemoryCache();
        services.AddDistributedMemoryCache(); // Replace with Redis/SQL for production
        services.AddSingleton<ICacheService, DistributedCacheService>();
        services.AddScoped<CacheInvalidationService>();

        // Event Bus - Uses in-memory by default for development
        services.Configure<Messaging.ServiceBusOptions>(configuration.GetSection(Messaging.ServiceBusOptions.SectionName));
        services.AddSingleton<Messaging.IEventBus, Messaging.InMemoryEventBus>();

        // Event handlers (OODA loop + domain events)
        services.AddScoped<Messaging.Handlers.ObservationEventHandler>();
        services.AddScoped<Messaging.Handlers.OrientationEventHandler>();
        services.AddScoped<Messaging.Handlers.DecisionEventHandler>();
        services.AddScoped<Messaging.Handlers.ActionEventHandler>();
        services.AddScoped<Messaging.Handlers.AtomIngestedEventHandler>();
        services.AddScoped<Messaging.Handlers.CacheInvalidatedEventHandler>();
        services.AddScoped<Messaging.Handlers.QuotaExceededEventHandler>();

        // Event bus hosted service - initializes subscriptions on startup
        services.AddHostedService<Messaging.EventBusHostedService>();

        // Security & Access Control
        services.AddSingleton<IAccessPolicyRule, TenantAccessPolicyRule>();
        services.AddSingleton<IAccessPolicyEngine, AccessPolicyEngine>();
        services.AddSingleton<IThrottleEvaluator, InMemoryThrottleEvaluator>();

        // Billing & Usage Tracking
        services.AddSingleton<IBillingConfigurationProvider, SqlBillingConfigurationProvider>();
        services.AddSingleton<IBillingMeter, UsageBillingMeter>();
        services.AddSingleton<IBillingUsageSink, SqlBillingUsageSink>();

        // Messaging (Service Broker) - concrete class
        services.AddSingleton<SqlMessageBroker>();

        // Graph operations
        services.AddScoped<IAtomGraphWriter, AtomGraphWriter>();

        // Centralized configuration service
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        return services;
    }

    /// <summary>
    /// Configures PII sanitization and redaction for logging, HTTP logging, and telemetry.
    /// Uses Microsoft.Extensions.Compliance.Redaction with custom data classifications.
    /// </summary>
    public static IServiceCollection AddHartonomousPiiRedaction(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        services.Configure<PiiSanitizationOptions>(
            configuration.GetSection("PiiSanitization"));

        var options = configuration.GetSection("PiiSanitization").Get<PiiSanitizationOptions>()
            ?? new PiiSanitizationOptions();

        // Core redaction services
        services.AddRedaction(redactionBuilder =>
        {
            // Public data - no redaction (we don't register a redactor, so it won't be redacted)

            // Private, Personal, and Financial data - mask with asterisks
            // Using custom StarRedactor for visible but anonymized data
            redactionBuilder.SetRedactor<StarRedactor>(
                new Microsoft.Extensions.Compliance.Classification.DataClassificationSet(
                    HartonomousDataClassifications.Private),
                new Microsoft.Extensions.Compliance.Classification.DataClassificationSet(
                    HartonomousDataClassifications.Personal),
                new Microsoft.Extensions.Compliance.Classification.DataClassificationSet(
                    HartonomousDataClassifications.Financial));

            // Sensitive and Health data - completely erase using ErasingRedactor
            // This is the fallback redactor, so any data without a specific redactor will be erased
            redactionBuilder.SetRedactor<ErasingRedactor>(
                new Microsoft.Extensions.Compliance.Classification.DataClassificationSet(
                    HartonomousDataClassifications.Sensitive),
                new Microsoft.Extensions.Compliance.Classification.DataClassificationSet(
                    HartonomousDataClassifications.Health));

            // Set fallback redactor for any unclassified sensitive data
            redactionBuilder.SetFallbackRedactor<ErasingRedactor>();
        });

        return services;
    }
}

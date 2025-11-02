using System;
using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Configuration;
using Hartonomous.Infrastructure.Services.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hartonomous.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering messaging services
/// </summary>
public static class MessagingServiceExtensions
{
    /// <summary>
    /// Registers Event Hub publisher services
    /// </summary>
    public static IServiceCollection AddEventHubPublisher(
        this IServiceCollection services,
        IConfiguration configuration,
        string? sectionName = null)
    {
        var section = configuration.GetSection(sectionName ?? EventHubOptions.SectionName);
        
        services.Configure<EventHubOptions>(section);
        services.AddSingleton<IEventPublisher, EventHubPublisher>();

        return services;
    }

    /// <summary>
    /// Registers Event Hub consumer services
    /// </summary>
    public static IServiceCollection AddEventHubConsumer(
        this IServiceCollection services,
        IConfiguration configuration,
        string? sectionName = null)
    {
        var section = configuration.GetSection(sectionName ?? EventHubOptions.SectionName);
        
        services.Configure<EventHubOptions>(section);
        services.AddSingleton<IEventConsumer, EventHubConsumer>();

        return services;
    }

    /// <summary>
    /// Registers both Event Hub publisher and consumer services
    /// </summary>
    public static IServiceCollection AddEventHub(
        this IServiceCollection services,
        IConfiguration configuration,
        string? sectionName = null)
    {
        services.AddEventHubPublisher(configuration, sectionName);
        services.AddEventHubConsumer(configuration, sectionName);

        return services;
    }
}

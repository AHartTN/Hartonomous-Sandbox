using System;
using Hartonomous.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;

namespace Hartonomous.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering Neo4j services
/// </summary>
public static class Neo4jServiceExtensions
{
    /// <summary>
    /// Registers Neo4j driver as a singleton service
    /// </summary>
    public static IServiceCollection AddNeo4j(
        this IServiceCollection services,
        IConfiguration configuration,
        string? sectionName = null)
    {
        var section = configuration.GetSection(sectionName ?? Neo4jOptions.SectionName);
        
        services.Configure<Neo4jOptions>(section);

        services.AddSingleton<IDriver>(sp =>
        {
            var options = section.Get<Neo4jOptions>() 
                ?? throw new InvalidOperationException("Neo4j configuration is missing");

            var uri = options.Uri ?? throw new InvalidOperationException("Neo4j URI is not configured");
            var username = options.Username ?? "neo4j";
            var password = options.Password ?? throw new InvalidOperationException("Neo4j password is not configured");

            var config = ConfigBuilder.Default()
                .WithMaxConnectionPoolSize(options.MaxConnectionPoolSize)
                .WithConnectionTimeout(TimeSpan.FromSeconds(options.ConnectionTimeoutSeconds))
                .Build();

            return GraphDatabase.Driver(uri, AuthTokens.Basic(username, password), config);
        });

        return services;
    }
}

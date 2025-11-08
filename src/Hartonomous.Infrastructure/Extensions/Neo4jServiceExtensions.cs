using System;
using Hartonomous.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
        services.PostConfigure<Neo4jOptions>(options =>
        {
            options.Uri = Environment.GetEnvironmentVariable("NEO4J_URI") ?? options.Uri;
            options.User = Environment.GetEnvironmentVariable("NEO4J_USER") ?? options.User;
            options.Password = Environment.GetEnvironmentVariable("NEO4J_PASSWORD") ?? options.Password;
        });

        services.AddSingleton<IDriver>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<Neo4jOptions>>().Value;

            if (string.IsNullOrWhiteSpace(options.Uri))
            {
                throw new InvalidOperationException("Neo4j URI is not configured");
            }

            if (string.IsNullOrWhiteSpace(options.User) || string.IsNullOrWhiteSpace(options.Password))
            {
                throw new InvalidOperationException("Neo4j credentials are not configured");
            }

            return GraphDatabase.Driver(
                options.Uri,
                AuthTokens.Basic(options.User, options.Password),
                builder =>
                {
                    builder.WithMaxConnectionPoolSize(options.MaxConnectionPoolSize);
                    builder.WithConnectionTimeout(TimeSpan.FromSeconds(options.ConnectionTimeoutSeconds));
                });
        });

        return services;
    }
}

using Hartonomous.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hartonomous.UnitTests.Infrastructure.TestFixtures;

/// <summary>
/// Provides in-memory database contexts for fast unit tests.
/// Each test gets an isolated database instance.
/// Thread-safe for parallel test execution.
/// </summary>
public class InMemoryDbContextFixture
{
    /// <summary>
    /// Creates a new in-memory database context with a unique database name.
    /// Use this for each test to ensure isolation.
    /// </summary>
    /// <returns>A new HartonomousDbContext using in-memory database</returns>
    public HartonomousDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HartonomousDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            .Options;

        return new HartonomousDbContext(options);
    }

    /// <summary>
    /// Creates a context with seed data for common test scenarios.
    /// </summary>
    /// <param name="seedData">Action to seed data</param>
    /// <returns>A new context with seeded data</returns>
    public HartonomousDbContext CreateContextWithData(Action<HartonomousDbContext> seedData)
    {
        var context = CreateContext();
        seedData(context);
        context.SaveChanges();
        return context;
    }
}

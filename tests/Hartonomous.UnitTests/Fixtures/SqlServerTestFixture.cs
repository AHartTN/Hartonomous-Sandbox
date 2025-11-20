using Hartonomous.Data.Entities;
using Hartonomous.Data.Entities.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hartonomous.UnitTests.Fixtures;

/// <summary>
/// Thread-safe, idempotent test fixture for SQL Server integration tests.
/// Uses EnsureDeleted/EnsureCreated pattern for guaranteed clean state.
/// Safe for parallel test execution with transaction-based isolation.
/// </summary>
public class SqlServerTestFixture : IDisposable
{
    private readonly string _connectionString;
    
    // Thread-safe initialization pattern for parallel test execution
    private static readonly object _lock = new();
    private static bool _databaseInitialized;

    public SqlServerTestFixture()
    {
        _connectionString = TestConfiguration.GetConnectionString();
        
        lock (_lock)
        {
            if (!_databaseInitialized)
            {
                InitializeDatabase();
                _databaseInitialized = true;
            }
        }
    }

    /// <summary>
    /// Idempotent database initialization: EnsureDeleted + EnsureCreated ensures clean state
    /// Safe to call multiple times, safe to run in parallel (protected by lock)
    /// </summary>
    private void InitializeDatabase()
    {
        using var context = CreateContext();
        
        // Idempotent: safe to call even if database doesn't exist
        context.Database.EnsureDeleted();
        
        // Creates fresh schema from current EF Core model
        context.Database.EnsureCreated();
        
        // Seed with stable, predictable reference data
        SeedDatabase(context);
    }

    /// <summary>
    /// Seeds database with consistent test data.
    /// Uses minimal data for predictable test queries.
    /// </summary>
    private void SeedDatabase(HartonomousDbContext context)
    {
        // Add minimal seed data - sample atoms for read-only tests
        var atom1 = new Atom
        {
            TenantId = 1,
            ContentHash = new byte[32] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32 },
            Modality = "text",
            SourceUri = "test://file-1.txt",
            ReferenceCount = 0
        };
        
        var atom2 = new Atom
        {
            TenantId = 1,
            ContentHash = new byte[32] { 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 },
            Modality = "image",
            SourceUri = "test://image-1.jpg",
            ReferenceCount = 0
        };
        
        context.Atoms.AddRange(atom1, atom2);
        
        context.SaveChanges();
    }

    /// <summary>
    /// Creates a new DbContext instance connected to the test database.
    /// Each test should create its own context and dispose it properly.
    /// For write tests, wrap in BeginTransaction() for automatic rollback.
    /// </summary>
    public HartonomousDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HartonomousDbContext>()
            .UseSqlServer(_connectionString, sqlOptions =>
            {
                sqlOptions.UseNetTopologySuite();  // Required for spatial geometry types
            })
            .EnableSensitiveDataLogging()  // Show parameter values in logs (test only!)
            .EnableDetailedErrors()         // Include query details in exceptions
            .Options;
        
        return new HartonomousDbContext(options);
    }

    /// <summary>
    /// Optional cleanup after all tests complete.
    /// Uncomment to delete test database when fixture is disposed.
    /// </summary>
    public void Dispose()
    {
        // Optional: Clean up database after all tests complete
        // Uncomment if you want to delete test database
        // using var context = CreateContext();
        // context.Database.EnsureDeleted();
    }
}

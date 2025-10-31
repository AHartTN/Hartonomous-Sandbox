using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Utilities;
using Hartonomous.Data;
using Hartonomous.Infrastructure.Data;
using Hartonomous.Infrastructure.Repositories;
using Hartonomous.Infrastructure.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using Xunit;

namespace Integration.Tests;

static class SqlScriptLoader
{
    private static readonly Lazy<string> RepoRootLazy = new(() =>
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "Hartonomous.sln")))
        {
            current = current.Parent;
        }

        if (current is null)
        {
            throw new InvalidOperationException("Unable to locate repository root (Hartonomous.sln not found).");
        }

        return current.FullName;
    });

    public static string RepositoryRoot => RepoRootLazy.Value;

    public static async Task ExecuteSqlFileAsync(DatabaseFacade database, string relativePath, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(database);

        var fullPath = Path.Combine(RepositoryRoot, relativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"SQL script not found: {relativePath}", fullPath);
        }

        var content = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);
        foreach (var batch in SplitSqlBatches(content))
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            await database.ExecuteSqlRawAsync(batch, cancellationToken).ConfigureAwait(false);
        }
    }

    private static IEnumerable<string> SplitSqlBatches(string sql)
    {
        var builder = new StringBuilder();
        using var reader = new StringReader(sql);
        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            if (Regex.IsMatch(line, "^\\s*GO\\s*$", RegexOptions.IgnoreCase))
            {
                yield return builder.ToString();
                builder.Clear();
                continue;
            }

            builder.AppendLine(line);
        }

        if (builder.Length > 0)
        {
            yield return builder.ToString();
        }
    }
}

/// <summary>
/// Shared SQL Server fixture that wires up the real infrastructure services against the configured database.
/// Tests throw <see cref="SkipException"/> when the database is unavailable.
/// </summary>
public sealed class SqlServerTestFixture : IAsyncLifetime
{
    private SqlConnection? _probeConnection;
    private ILoggerFactory? _loggerFactory;

    public SqlServerTestFixture()
    {
        ConnectionString = Environment.GetEnvironmentVariable("HARTONOMOUS_SQL_CONNECTION")
            ?? "Server=localhost;Database=Hartonomous;Trusted_Connection=True;TrustServerCertificate=True;";
    }

    public string ConnectionString { get; }

    public string SkipReason { get; private set; } = string.Empty;
    public HartonomousDbContext? DbContext { get; private set; }

    public bool IsAvailable { get; private set; }

    public AtomEmbeddingRepository? AtomEmbeddings { get; private set; }

    public InferenceOrchestrator? InferenceService { get; private set; }

    public SpatialInferenceService? SpatialService { get; private set; }

    public StudentModelService? StudentModelService { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            _probeConnection = new SqlConnection(ConnectionString);
            await _probeConnection.OpenAsync();
            await _probeConnection.CloseAsync();

            var dbOptions = new DbContextOptionsBuilder<HartonomousDbContext>()
                .UseSqlServer(ConnectionString, sql => sql.UseNetTopologySuite())
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                .Options;

            DbContext = new HartonomousDbContext(dbOptions);

            await DbContext.Database.MigrateAsync().ConfigureAwait(false);

            // Probe the DbContext connection early so failures surface as skips.
            await DbContext.Database.OpenConnectionAsync().ConfigureAwait(false);
            await DbContext.Database.CloseConnectionAsync().ConfigureAwait(false);

            await EnsureSpatialInfrastructureAsync(CancellationToken.None).ConfigureAwait(false);
            await EnsureIntegrationSeedDataAsync(CancellationToken.None).ConfigureAwait(false);

            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Warning);
            });

            var optionsMonitor = new StaticOptionsMonitor<SqlServerOptions>(new SqlServerOptions
            {
                ConnectionString = ConnectionString,
                CommandTimeoutSeconds = 120
            });

            var connectionFactory = new SqlServerConnectionFactory(optionsMonitor, _loggerFactory.CreateLogger<SqlServerConnectionFactory>());
            var sqlExecutor = new SqlCommandExecutor(connectionFactory, optionsMonitor, _loggerFactory.CreateLogger<SqlCommandExecutor>());

            AtomEmbeddings = new AtomEmbeddingRepository(DbContext, sqlExecutor);
            var modelLayerRepository = new ModelLayerRepository(DbContext);
            StudentModelService = new StudentModelService(DbContext, modelLayerRepository);
            InferenceService = new InferenceOrchestrator(DbContext, AtomEmbeddings, sqlExecutor, _loggerFactory.CreateLogger<InferenceOrchestrator>());
            SpatialService = new SpatialInferenceService(DbContext);

            var resolvedConnectionString = DbContext.Database.GetConnectionString();
            if (string.IsNullOrWhiteSpace(resolvedConnectionString))
            {
                throw new InvalidOperationException("DbContext connection string was not initialised.");
            }

            IsAvailable = true;
        }
        catch (Exception ex)
        {
            SkipReason = $"SQL Server integration environment unavailable: {ex}";
            IsAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        DbContext?.Dispose();
        if (_probeConnection is not null)
        {
            await _probeConnection.DisposeAsync();
        }

        _loggerFactory?.Dispose();
    }

    private async Task EnsureSpatialInfrastructureAsync(CancellationToken cancellationToken)
    {
        if (DbContext is null)
        {
            throw new InvalidOperationException("DbContext must be initialised before ensuring spatial infrastructure.");
        }

        var database = DbContext.Database;

        const string spatialIndexSql = """
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'idx_spatial_embedding'
      AND object_id = OBJECT_ID(N'dbo.TokenEmbeddingsGeo')
)
BEGIN
    DECLARE @sql NVARCHAR(MAX) = N'
        CREATE SPATIAL INDEX idx_spatial_embedding
        ON dbo.TokenEmbeddingsGeo(SpatialProjection)
        USING GEOMETRY_GRID
        WITH (
            BOUNDING_BOX = (-100, -100, 100, 100),
            GRIDS = (LEVEL_1 = HIGH, LEVEL_2 = HIGH, LEVEL_3 = MEDIUM, LEVEL_4 = LOW),
            CELLS_PER_OBJECT = 16
        );';
    EXEC(@sql);
END;
""";
        await database.ExecuteSqlRawAsync(spatialIndexSql, cancellationToken).ConfigureAwait(false);

        const string seedTokensSql = """
IF NOT EXISTS (SELECT 1 FROM dbo.TokenEmbeddingsGeo)
BEGIN
    INSERT INTO dbo.TokenEmbeddingsGeo (TokenText, SpatialProjection, CoarseSpatial, FineSpatial, Frequency)
    VALUES
    ('the', geometry::STGeomFromText('POINT (0.10 0.20 0.10)', 0), geometry::STGeomFromText('POINT (0 0 0)', 0), geometry::STGeomFromText('POINT (0.10 0.20 0.10)', 0), 512),
    ('is', geometry::STGeomFromText('POINT (0.15 0.18 0.12)', 0), geometry::STGeomFromText('POINT (0 0 0)', 0), geometry::STGeomFromText('POINT (0.15 0.18 0.12)', 0), 384),
    ('machine', geometry::STGeomFromText('POINT (5.2 3.1 1.8)', 0), geometry::STGeomFromText('POINT (5 3 2)', 0), geometry::STGeomFromText('POINT (5.2 3.1 1.8)', 0), 120),
    ('learning', geometry::STGeomFromText('POINT (5.5 3.3 2.1)', 0), geometry::STGeomFromText('POINT (5 3 2)', 0), geometry::STGeomFromText('POINT (5.5 3.3 2.1)', 0), 110),
    ('database', geometry::STGeomFromText('POINT (-3.1 4.2 -1.5)', 0), geometry::STGeomFromText('POINT (-3 4 -2)', 0), geometry::STGeomFromText('POINT (-3.1 4.2 -1.5)', 0), 95),
    ('query', geometry::STGeomFromText('POINT (-2.8 4.5 -1.3)', 0), geometry::STGeomFromText('POINT (-3 4 -2)', 0), geometry::STGeomFromText('POINT (-2.8 4.5 -1.3)', 0), 88),
    ('neural', geometry::STGeomFromText('POINT (5.8 2.9 2.3)', 0), geometry::STGeomFromText('POINT (5 3 2)', 0), geometry::STGeomFromText('POINT (5.8 2.9 2.3)', 0), 140),
    ('network', geometry::STGeomFromText('POINT (6.1 3.2 2.5)', 0), geometry::STGeomFromText('POINT (5 3 2)', 0), geometry::STGeomFromText('POINT (6.1 3.2 2.5)', 0), 135);
END;
""";
        await database.ExecuteSqlRawAsync(seedTokensSql, cancellationToken).ConfigureAwait(false);

        const string ensureAnchorsSql = """
IF NOT EXISTS (SELECT 1 FROM dbo.SpatialAnchors)
BEGIN
    EXEC dbo.sp_InitializeSpatialAnchors;
END;
""";
        await database.ExecuteSqlRawAsync(ensureAnchorsSql, cancellationToken).ConfigureAwait(false);

    }

    private async Task EnsureIntegrationSeedDataAsync(CancellationToken cancellationToken)
    {
        if (DbContext is null)
        {
            throw new InvalidOperationException("DbContext must be initialised before seeding integration data.");
        }

        var model = await DbContext.Models
            .OrderBy(m => m.ModelId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (model is null)
        {
            model = new Model
            {
                ModelName = "IntegrationBaseline",
                ModelType = "transformer",
                Architecture = "integration-test",
                Config = "{\"source\":\"integration-tests\"}",
                ParameterCount = 1_000_000,
                IngestionDate = DateTime.UtcNow
            };

            DbContext.Models.Add(model);
            await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var existingLayerCount = await DbContext.ModelLayers
            .CountAsync(l => l.ModelId == model.ModelId, cancellationToken)
            .ConfigureAwait(false);

        if (existingLayerCount < 4)
        {
            var layersToAdd = Enumerable.Range(existingLayerCount, 4 - existingLayerCount)
                .Select(idx => new ModelLayer
                {
                    ModelId = model.ModelId,
                    LayerIdx = idx,
                    LayerName = $"Layer_{idx}",
                    LayerType = idx % 2 == 0 ? "attention" : "feedforward",
                    WeightsGeometry = CreateLayerGeometry(idx),
                    TensorShape = "[4,4]",
                    TensorDtype = "float32",
                    ParameterCount = 16,
                    CacheHitRate = 0.75,
                    AvgComputeTimeMs = 1.5 + idx
                })
                .ToList();

            if (layersToAdd.Count > 0)
            {
                await DbContext.ModelLayers.AddRangeAsync(layersToAdd, cancellationToken).ConfigureAwait(false);
                await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        if (!await DbContext.AtomEmbeddings.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var embeddings = new List<AtomEmbedding>();

            for (var index = 0; index < 3; index++)
            {
                var text = $"Integration sample text {index + 1}";
                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));

                var atom = await DbContext.Atoms
                    .FirstOrDefaultAsync(a => a.ContentHash == hash, cancellationToken)
                    .ConfigureAwait(false);

                if (atom is null)
                {
                    atom = new Atom
                    {
                        ContentHash = hash,
                        Modality = "text",
                        Subtype = "document",
                        SourceUri = "integration://seed",
                        SourceType = "integration-test",
                        CanonicalText = text,
                        Metadata = "{\"source\":\"integration-tests\"}",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        ReferenceCount = 1
                    };

                    DbContext.Atoms.Add(atom);
                }
                else
                {
                    atom.ReferenceCount = Math.Max(atom.ReferenceCount, 1);
                }

                var dense = CreateSampleVector(index);
                var padded = VectorUtility.PadToSqlLength(dense, out var usedPadding);

                var embedding = new AtomEmbedding
                {
                    Atom = atom,
                    ModelId = model.ModelId,
                    EmbeddingType = "text",
                    Dimension = dense.Length,
                    EmbeddingVector = new SqlVector<float>(padded),
                    UsesMaxDimensionPadding = usedPadding,
                    SpatialGeometry = CreateSpatialPoint(index, false),
                    SpatialCoarse = CreateSpatialPoint(index, true),
                    Metadata = "{\"ingestion\":\"integration-tests\"}",
                    CreatedAt = DateTime.UtcNow
                };

                embeddings.Add(embedding);
            }

            await DbContext.AtomEmbeddings.AddRangeAsync(embeddings, cancellationToken).ConfigureAwait(false);
            await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var missingSpatial = await DbContext.AtomEmbeddings
                .Where(e => e.SpatialGeometry == null || e.SpatialCoarse == null)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var updated = false;
            for (var i = 0; i < missingSpatial.Count; i++)
            {
                var embedding = missingSpatial[i];
                embedding.SpatialGeometry ??= CreateSpatialPoint(i, false);
                embedding.SpatialCoarse ??= CreateSpatialPoint(i, true);
                updated = true;
            }

            if (updated)
            {
                await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static LineString CreateLayerGeometry(int layerIndex)
    {
        var coordinates = Enumerable.Range(0, 5)
            .Select(idx => new CoordinateZ(idx, Math.Sin((layerIndex + 1) * (idx + 1) * 0.1), layerIndex * 0.05))
            .ToArray();

        return new LineString(coordinates) { SRID = 0 };
    }

    private static float[] CreateSampleVector(int seed)
    {
        var values = new float[32];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = (float)Math.Sin((seed + 1) * (i + 1) * 0.25);
        }

        return values;
    }

    private static Point CreateSpatialPoint(int index, bool coarse)
    {
        var baseX = 10 + index * 1.5;
        var baseY = -5 + index * 0.75;
        var baseZ = 2 + index * 0.4;

        if (coarse)
        {
            baseX *= 0.5;
            baseY *= 0.5;
            baseZ *= 0.5;
        }

        return new Point(new CoordinateZ(baseX, baseY, baseZ)) { SRID = 0 };
    }

    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T> where T : class
    {
        private readonly T _value;

        public StaticOptionsMonitor(T value)
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public T CurrentValue => _value;

        public T Get(string? name) => _value;

        public IDisposable OnChange(Action<T, string> listener) => NullDisposable.Instance;

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}

public abstract class IntegrationTestBase : IClassFixture<SqlServerTestFixture>
{
    protected IntegrationTestBase(SqlServerTestFixture fixture)
    {
        Fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        if (!fixture.IsAvailable)
        {
            Assert.True(fixture.IsAvailable, fixture.SkipReason);
        }

        fixture.DbContext?.Database.SetConnectionString(fixture.ConnectionString);
    }

    protected SqlServerTestFixture Fixture { get; }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Utilities;
using Hartonomous.Testing.Common;
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

namespace Hartonomous.IntegrationTests;

internal static class SqlScriptLoader
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

            await DbContext.Database.OpenConnectionAsync().ConfigureAwait(false);
            await DbContext.Database.CloseConnectionAsync().ConfigureAwait(false);

            await EnsureStoredProceduresUpToDateAsync(CancellationToken.None).ConfigureAwait(false);
            await EnsureIntegrationSeedDataAsync(CancellationToken.None).ConfigureAwait(false);
            await EnsureSpatialInfrastructureAsync(CancellationToken.None).ConfigureAwait(false);

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

            var atomEmbeddingLogger = _loggerFactory.CreateLogger<AtomEmbeddingRepository>();
            AtomEmbeddings = new AtomEmbeddingRepository(DbContext, atomEmbeddingLogger, sqlExecutor);

            var modelLayerLogger = _loggerFactory.CreateLogger<ModelLayerRepository>();
            var modelLayerRepository = new ModelLayerRepository(DbContext, modelLayerLogger);
            StudentModelService = new StudentModelService(DbContext, modelLayerRepository);
            InferenceService = new InferenceOrchestrator(DbContext, AtomEmbeddings, sqlExecutor, _loggerFactory.CreateLogger<InferenceOrchestrator>());
            SpatialService = new SpatialInferenceService(sqlExecutor);

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

    private async Task EnsureStoredProceduresUpToDateAsync(CancellationToken cancellationToken)
    {
        if (DbContext is null)
        {
            throw new InvalidOperationException("DbContext must be initialised before refreshing stored procedures.");
        }

        const string multiResolutionSearchSql = """
CREATE OR ALTER PROCEDURE dbo.sp_MultiResolutionSearch
    @query_x FLOAT,
    @query_y FLOAT,
    @query_z FLOAT,
    @coarse_candidates INT = 1000,
    @fine_candidates INT = 100,
    @final_top_k INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @query_wkt NVARCHAR(200) = CONCAT('POINT (', @query_x, ' ', @query_y, ' ', @query_z, ')');
    DECLARE @query_pt GEOMETRY = geometry::STGeomFromText(@query_wkt, 0);

    DECLARE @coarse_results TABLE (AtomEmbeddingId BIGINT PRIMARY KEY);

    INSERT INTO @coarse_results (AtomEmbeddingId)
    SELECT TOP (@coarse_candidates) ae.AtomEmbeddingId
    FROM dbo.AtomEmbeddings AS ae
    WHERE ae.SpatialCoarse IS NOT NULL
    ORDER BY ae.SpatialCoarse.STDistance(@query_pt);

    DECLARE @fine_results TABLE (AtomEmbeddingId BIGINT PRIMARY KEY);

    INSERT INTO @fine_results (AtomEmbeddingId)
    SELECT TOP (@fine_candidates) ae.AtomEmbeddingId
    FROM dbo.AtomEmbeddings AS ae
    INNER JOIN @coarse_results AS cr ON cr.AtomEmbeddingId = ae.AtomEmbeddingId
    WHERE ae.SpatialGeometry IS NOT NULL
    ORDER BY ae.SpatialGeometry.STDistance(@query_pt);

    SELECT TOP (@final_top_k)
        ae.AtomEmbeddingId,
        ae.AtomId,
        a.Modality,
        a.Subtype,
        a.SourceType,
        a.SourceUri,
        a.CanonicalText,
        ae.EmbeddingType,
        ae.ModelId,
        ae.SpatialGeometry.STDistance(@query_pt) AS SpatialDistance,
        ae.SpatialCoarse.STDistance(@query_pt) AS CoarseDistance
    FROM dbo.AtomEmbeddings AS ae
    INNER JOIN @fine_results AS fr ON fr.AtomEmbeddingId = ae.AtomEmbeddingId
    INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
    ORDER BY SpatialDistance ASC;
END;
""";

        const string recomputeSpatialSql = """
CREATE OR ALTER PROCEDURE dbo.sp_RecomputeAllSpatialProjections
AS
BEGIN
    SET NOCOUNT ON;

    EXEC dbo.sp_InitializeSpatialAnchors;

    DECLARE @embeddingId BIGINT;
    DECLARE @vector VECTOR(1998);
    DECLARE @dimension INT;
    DECLARE @x FLOAT, @y FLOAT, @z FLOAT;

    DECLARE embedding_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT AtomEmbeddingId, EmbeddingVector, Dimension
        FROM dbo.AtomEmbeddings
        WHERE EmbeddingVector IS NOT NULL;

    OPEN embedding_cursor;
    FETCH NEXT FROM embedding_cursor INTO @embeddingId, @vector, @dimension;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF (@dimension <= 0 OR @dimension > 1998)
        BEGIN
            SET @dimension = 1998;
        END;

        EXEC dbo.sp_ComputeSpatialProjection
            @input_vector = @vector,
            @input_dimension = @dimension,
            @output_x = @x OUTPUT,
            @output_y = @y OUTPUT,
            @output_z = @z OUTPUT;

        DECLARE @geometryWkt NVARCHAR(200) =
            'POINT (' + CONVERT(NVARCHAR(50), @x) + ' ' +
                            CONVERT(NVARCHAR(50), @y) + ' ' +
                            CONVERT(NVARCHAR(50), @z) + ')';

        DECLARE @coarseWkt NVARCHAR(200) =
            'POINT (' + CONVERT(NVARCHAR(50), FLOOR(@x)) + ' ' +
                            CONVERT(NVARCHAR(50), FLOOR(@y)) + ' ' +
                            CONVERT(NVARCHAR(50), FLOOR(@z)) + ')';

        UPDATE dbo.AtomEmbeddings
        SET
            SpatialGeometry = geometry::STGeomFromText(@geometryWkt, 0),
            SpatialCoarse = geometry::STGeomFromText(@coarseWkt, 0)
        WHERE AtomEmbeddingId = @embeddingId;

        FETCH NEXT FROM embedding_cursor INTO @embeddingId, @vector, @dimension;
    END;

    CLOSE embedding_cursor;
    DEALLOCATE embedding_cursor;
END;
""";

        await DbContext.Database.ExecuteSqlRawAsync(multiResolutionSearchSql, cancellationToken).ConfigureAwait(false);
        await DbContext.Database.ExecuteSqlRawAsync(recomputeSpatialSql, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureSpatialInfrastructureAsync(CancellationToken cancellationToken)
    {
        if (DbContext is null)
        {
            throw new InvalidOperationException("DbContext must be initialised before ensuring spatial infrastructure.");
        }

        var database = DbContext.Database;

        const string ensureTableSql = """
IF OBJECT_ID(N'dbo.TokenEmbeddingsGeo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TokenEmbeddingsGeo
    (
        TokenEmbeddingsGeoId BIGINT IDENTITY(1,1) PRIMARY KEY,
        TokenText NVARCHAR(128) NOT NULL,
        SpatialProjection GEOMETRY NOT NULL,
        CoarseSpatial GEOMETRY NULL,
        FineSpatial GEOMETRY NULL,
        Frequency INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NULL
    );

    CREATE UNIQUE INDEX UX_TokenEmbeddingsGeo_TokenText
        ON dbo.TokenEmbeddingsGeo(TokenText);
END;
""";
        await database.ExecuteSqlRawAsync(ensureTableSql, cancellationToken).ConfigureAwait(false);

        const string spatialIndexSql = """
IF OBJECT_ID(N'dbo.TokenEmbeddingsGeo', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'idx_spatial_embedding'
          AND object_id = OBJECT_ID(N'dbo.TokenEmbeddingsGeo')
    )
    BEGIN
        DROP INDEX idx_spatial_embedding ON dbo.TokenEmbeddingsGeo;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_TokenEmbeddingsGeo_SpatialProjection'
          AND object_id = OBJECT_ID(N'dbo.TokenEmbeddingsGeo')
    )
    BEGIN
        DECLARE @sql NVARCHAR(MAX) = N'
            CREATE SPATIAL INDEX IX_TokenEmbeddingsGeo_SpatialProjection
            ON dbo.TokenEmbeddingsGeo(SpatialProjection)
            USING GEOMETRY_GRID
            WITH (
                BOUNDING_BOX = (-100, -100, 100, 100),
                GRIDS = (LEVEL_1 = HIGH, LEVEL_2 = HIGH, LEVEL_3 = MEDIUM, LEVEL_4 = LOW),
                CELLS_PER_OBJECT = 16
            );';
        EXEC(@sql);
    END;
END;
""";
        await database.ExecuteSqlRawAsync(spatialIndexSql, cancellationToken).ConfigureAwait(false);

        const string seedTokensSql = """
IF OBJECT_ID(N'dbo.TokenEmbeddingsGeo', N'U') IS NOT NULL
BEGIN
    DECLARE @SeedTokens TABLE
    (
        TokenText NVARCHAR(128),
        SpatialProjectionWkt NVARCHAR(200),
        CoarseSpatialWkt NVARCHAR(200),
        FineSpatialWkt NVARCHAR(200),
        Frequency INT
    );

    INSERT INTO @SeedTokens (TokenText, SpatialProjectionWkt, CoarseSpatialWkt, FineSpatialWkt, Frequency) VALUES
    (N'the', N'POINT (0.10 0.20 0.10)', N'POINT (0 0 0)', N'POINT (0.10 0.20 0.10)', 512),
    (N'is', N'POINT (0.15 0.18 0.12)', N'POINT (0 0 0)', N'POINT (0.15 0.18 0.12)', 384),
    (N'machine', N'POINT (5.2 3.1 1.8)', N'POINT (5 3 2)', N'POINT (5.2 3.1 1.8)', 120),
    (N'learning', N'POINT (5.5 3.3 2.1)', N'POINT (5 3 2)', N'POINT (5.5 3.3 2.1)', 110),
    (N'database', N'POINT (-3.1 4.2 -1.5)', N'POINT (-3 4 -2)', N'POINT (-3.1 4.2 -1.5)', 95),
    (N'query', N'POINT (-2.8 4.5 -1.3)', N'POINT (-3 4 -2)', N'POINT (-2.8 4.5 -1.3)', 88),
    (N'neural', N'POINT (5.8 2.9 2.3)', N'POINT (5 3 2)', N'POINT (5.8 2.9 2.3)', 140),
    (N'network', N'POINT (6.1 3.2 2.5)', N'POINT (5 3 2)', N'POINT (6.1 3.2 2.5)', 135);

    MERGE dbo.TokenEmbeddingsGeo AS target
    USING (
        SELECT
            TokenText,
            SpatialProjectionWkt,
            CoarseSpatialWkt,
            FineSpatialWkt,
            Frequency
        FROM @SeedTokens
    ) AS source
        ON target.TokenText = source.TokenText
    WHEN MATCHED THEN UPDATE SET
        SpatialProjection = geometry::STGeomFromText(source.SpatialProjectionWkt, 0),
        CoarseSpatial = geometry::STGeomFromText(source.CoarseSpatialWkt, 0),
        FineSpatial = geometry::STGeomFromText(source.FineSpatialWkt, 0),
        Frequency = source.Frequency,
        UpdatedAt = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN INSERT (TokenText, SpatialProjection, CoarseSpatial, FineSpatial, Frequency)
    VALUES (
        source.TokenText,
        geometry::STGeomFromText(source.SpatialProjectionWkt, 0),
        geometry::STGeomFromText(source.CoarseSpatialWkt, 0),
        geometry::STGeomFromText(source.FineSpatialWkt, 0),
        source.Frequency
    );
END;
""";
        await database.ExecuteSqlRawAsync(seedTokensSql, cancellationToken).ConfigureAwait(false);

        const string ensureAnchorsSql = """
IF OBJECT_ID(N'dbo.TokenEmbeddingsGeo', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.SpatialAnchors)
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

        // Check if seed data already exists to prevent duplicate key violations
        var existingDataCount = await DbContext.Atoms.CountAsync(cancellationToken).ConfigureAwait(false);
        if (existingDataCount > 0)
        {
            return; // Data already seeded
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

        var sampleTexts = new[]
        {
            TestData.Text.SampleContent,
            "Integration sample text 2",
            "Integration sample text 3"
        };

        var saveRequired = false;

        for (var index = 0; index < sampleTexts.Length; index++)
        {
            var text = sampleTexts[index];
            var contentHash = SHA256.HashData(Encoding.UTF8.GetBytes(text));

            var atom = await DbContext.Atoms
                .FirstOrDefaultAsync(a => a.ContentHash == contentHash, cancellationToken)
                .ConfigureAwait(false);

            if (atom is null)
            {
                atom = new Atom
                {
                    ContentHash = contentHash,
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
                await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var atomUpdated = false;

                if (!string.Equals(atom.CanonicalText, text, StringComparison.Ordinal))
                {
                    atom.CanonicalText = text;
                    atomUpdated = true;
                }

                if (!string.Equals(atom.SourceType, "integration-test", StringComparison.Ordinal))
                {
                    atom.SourceType = "integration-test";
                    atomUpdated = true;
                }

                if (atom.ReferenceCount < 1)
                {
                    atom.ReferenceCount = 1;
                    atomUpdated = true;
                }

                if (!atom.IsActive)
                {
                    atom.IsActive = true;
                    atomUpdated = true;
                }

                if (atomUpdated)
                {
                    saveRequired = true;
                }
            }

            var embedding = await DbContext.AtomEmbeddings
                .FirstOrDefaultAsync(e => e.AtomId == atom.AtomId && e.ModelId == model.ModelId, cancellationToken)
                .ConfigureAwait(false);

            if (embedding is null)
            {
                var dense = CreateSampleVector(index);
                var padded = VectorUtility.PadToSqlLength(dense, out var usedPadding);

                embedding = new AtomEmbedding
                {
                    AtomId = atom.AtomId,
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

                await DbContext.AtomEmbeddings.AddAsync(embedding, cancellationToken).ConfigureAwait(false);
                saveRequired = true;
            }
            else
            {
                var updated = false;

                if (embedding.SpatialGeometry is null)
                {
                    embedding.SpatialGeometry = CreateSpatialPoint(index, false);
                    updated = true;
                }

                if (embedding.SpatialCoarse is null)
                {
                    embedding.SpatialCoarse = CreateSpatialPoint(index, true);
                    updated = true;
                }

                if (embedding.Dimension <= 0)
                {
                    embedding.Dimension = 32;
                    updated = true;
                }

                if (embedding.EmbeddingVector is null)
                {
                    var dense = CreateSampleVector(index);
                    var padded = VectorUtility.PadToSqlLength(dense, out var usedPadding);
                    embedding.EmbeddingVector = new SqlVector<float>(padded);
                    embedding.Dimension = dense.Length;
                    embedding.UsesMaxDimensionPadding = usedPadding;
                    updated = true;
                }

                if (updated)
                {
                    saveRequired = true;
                }
            }
        }

        if (saveRequired)
        {
            await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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

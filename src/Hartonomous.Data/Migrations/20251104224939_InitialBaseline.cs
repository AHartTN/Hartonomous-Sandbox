using System;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Hartonomous.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.EnsureSchema(
                name: "provenance");

            migrationBuilder.CreateTable(
                name: "AtomicAudioSamples",
                columns: table => new
                {
                    SampleHash = table.Column<byte[]>(type: "BINARY(32)", nullable: false),
                    AmplitudeNormalized = table.Column<float>(type: "REAL", nullable: false),
                    AmplitudeInt16 = table.Column<short>(type: "SMALLINT", nullable: false),
                    ReferenceCount = table.Column<long>(type: "BIGINT", nullable: false, defaultValue: 0L),
                    FirstSeen = table.Column<DateTime>(type: "DATETIME2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    LastReferenced = table.Column<DateTime>(type: "DATETIME2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtomicAudioSamples", x => x.SampleHash);
                });

            migrationBuilder.CreateTable(
                name: "AtomicPixels",
                columns: table => new
                {
                    PixelHash = table.Column<byte[]>(type: "BINARY(32)", nullable: false),
                    R = table.Column<byte>(type: "TINYINT", nullable: false),
                    G = table.Column<byte>(type: "TINYINT", nullable: false),
                    B = table.Column<byte>(type: "TINYINT", nullable: false),
                    A = table.Column<byte>(type: "TINYINT", nullable: false, defaultValue: (byte)255),
                    ColorPoint = table.Column<Point>(type: "GEOMETRY", nullable: true),
                    ReferenceCount = table.Column<long>(type: "BIGINT", nullable: false, defaultValue: 0L),
                    FirstSeen = table.Column<DateTime>(type: "DATETIME2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    LastReferenced = table.Column<DateTime>(type: "DATETIME2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtomicPixels", x => x.PixelHash);
                });

            migrationBuilder.CreateTable(
                name: "AtomicTextTokens",
                columns: table => new
                {
                    TokenId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TokenHash = table.Column<byte[]>(type: "BINARY(32)", nullable: false),
                    TokenText = table.Column<string>(type: "NVARCHAR(200)", nullable: false),
                    TokenLength = table.Column<int>(type: "int", nullable: false),
                    TokenEmbedding = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true),
                    EmbeddingModel = table.Column<string>(type: "NVARCHAR(100)", nullable: true),
                    VocabId = table.Column<int>(type: "int", nullable: true),
                    ReferenceCount = table.Column<long>(type: "BIGINT", nullable: false, defaultValue: 0L),
                    FirstSeen = table.Column<DateTime>(type: "DATETIME2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    LastReferenced = table.Column<DateTime>(type: "DATETIME2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtomicTextTokens", x => x.TokenId);
                });

            migrationBuilder.CreateTable(
                name: "Atoms",
                columns: table => new
                {
                    AtomId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContentHash = table.Column<byte[]>(type: "binary(32)", nullable: false),
                    Modality = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Subtype = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SourceUri = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CanonicalText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PayloadLocator = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Metadata = table.Column<string>(type: "JSON", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ReferenceCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    SpatialKey = table.Column<Point>(type: "geometry", nullable: true),
                    ComponentStream = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Atoms", x => x.AtomId);
                });

            migrationBuilder.CreateTable(
                name: "AudioData",
                schema: "dbo",
                columns: table => new
                {
                    AudioId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourcePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SampleRate = table.Column<int>(type: "int", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    NumChannels = table.Column<byte>(type: "tinyint", nullable: false),
                    Format = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Spectrogram = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    MelSpectrogram = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    WaveformLeft = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    WaveformRight = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    GlobalEmbedding = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true),
                    GlobalEmbeddingDim = table.Column<int>(type: "int", nullable: true),
                    Metadata = table.Column<string>(type: "JSON", nullable: true),
                    IngestionDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioData", x => x.AudioId);
                });

            migrationBuilder.CreateTable(
                name: "BillingRatePlans",
                columns: table => new
                {
                    RatePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PlanCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, defaultValue: ""),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, defaultValue: ""),
                    Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DefaultRate = table.Column<decimal>(type: "decimal(18,6)", nullable: false, defaultValue: 0.01m),
                    MonthlyFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    UnitPricePerDcu = table.Column<decimal>(type: "decimal(18,6)", nullable: false, defaultValue: 0.00008m),
                    IncludedPublicStorageGb = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    IncludedPrivateStorageGb = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    IncludedSeatCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    AllowsPrivateData = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CanQueryPublicCorpus = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingRatePlans", x => x.RatePlanId);
                });

            migrationBuilder.CreateTable(
                name: "CodeAtoms",
                schema: "dbo",
                columns: table => new
                {
                    CodeAtomId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Framework = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CodeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Embedding = table.Column<Geometry>(type: "geometry", nullable: true),
                    EmbeddingDimension = table.Column<int>(type: "int", nullable: true),
                    TestResults = table.Column<string>(type: "JSON", nullable: true),
                    QualityScore = table.Column<float>(type: "real", nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CodeHash = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: true),
                    SourceUri = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Tags = table.Column<string>(type: "JSON", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeAtoms", x => x.CodeAtomId);
                });

            migrationBuilder.CreateTable(
                name: "DeduplicationPolicies",
                columns: table => new
                {
                    DeduplicationPolicyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SemanticThreshold = table.Column<double>(type: "float", nullable: true),
                    SpatialThreshold = table.Column<double>(type: "float", nullable: true),
                    Metadata = table.Column<string>(type: "JSON", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeduplicationPolicies", x => x.DeduplicationPolicyId);
                });

            migrationBuilder.CreateTable(
                name: "Embeddings_Production",
                columns: table => new
                {
                    EmbeddingId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmbeddingFull = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true),
                    EmbeddingModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SpatialProjX = table.Column<double>(type: "float", nullable: true),
                    SpatialProjY = table.Column<double>(type: "float", nullable: true),
                    SpatialProjZ = table.Column<double>(type: "float", nullable: true),
                    SpatialGeometry = table.Column<Point>(type: "geometry", nullable: true),
                    SpatialCoarse = table.Column<Point>(type: "geometry", nullable: true),
                    Dimension = table.Column<int>(type: "int", nullable: false, defaultValue: 768),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    AccessCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastAccessed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Embeddings_Production", x => x.EmbeddingId);
                });

            migrationBuilder.CreateTable(
                name: "GenerationStreams",
                schema: "provenance",
                columns: table => new
                {
                    StreamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Stream = table.Column<byte[]>(type: "provenance.AtomicStream", nullable: false),
                    PayloadSizeBytes = table.Column<long>(type: "bigint", nullable: false, computedColumnSql: "CONVERT(BIGINT, DATALENGTH([Stream]))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenerationStreams", x => x.StreamId);
                });

            migrationBuilder.CreateTable(
                name: "Images",
                schema: "dbo",
                columns: table => new
                {
                    ImageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourcePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    Channels = table.Column<int>(type: "int", nullable: false),
                    Format = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PixelCloud = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    EdgeMap = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    ObjectRegions = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    SaliencyRegions = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    GlobalEmbedding = table.Column<SqlVector<float>>(type: "VECTOR(1536)", nullable: true),
                    GlobalEmbeddingDim = table.Column<int>(type: "int", nullable: true),
                    Metadata = table.Column<string>(type: "JSON", nullable: true),
                    IngestionDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "SYSUTCDATETIME()"),
                    LastAccessed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AccessCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.ImageId);
                });

            migrationBuilder.CreateTable(
                name: "IngestionJobs",
                columns: table => new
                {
                    IngestionJobId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PipelineName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SourceUri = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Metadata = table.Column<string>(type: "JSON", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestionJobs", x => x.IngestionJobId);
                });

            migrationBuilder.CreateTable(
                name: "Models",
                columns: table => new
                {
                    ModelId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModelType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Architecture = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Config = table.Column<string>(type: "JSON", nullable: true),
                    ParameterCount = table.Column<long>(type: "bigint", nullable: true),
                    IngestionDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    LastUsed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsageCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    AverageInferenceMs = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Models", x => x.ModelId);
                });

            migrationBuilder.CreateTable(
                name: "TextDocuments",
                schema: "dbo",
                columns: table => new
                {
                    DocId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourcePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RawText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CharCount = table.Column<int>(type: "int", nullable: true),
                    WordCount = table.Column<int>(type: "int", nullable: true),
                    GlobalEmbedding = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true),
                    GlobalEmbeddingDim = table.Column<int>(type: "int", nullable: true),
                    TopicVector = table.Column<SqlVector<float>>(type: "VECTOR(100)", nullable: true),
                    SentimentScore = table.Column<float>(type: "real", nullable: true),
                    Toxicity = table.Column<float>(type: "real", nullable: true),
                    Metadata = table.Column<string>(type: "JSON", nullable: true),
                    IngestionDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "SYSUTCDATETIME()"),
                    LastAccessed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AccessCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TextDocuments", x => x.DocId);
                });

            migrationBuilder.CreateTable(
                name: "Videos",
                schema: "dbo",
                columns: table => new
                {
                    VideoId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourcePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Fps = table.Column<int>(type: "int", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    ResolutionWidth = table.Column<int>(type: "int", nullable: false),
                    ResolutionHeight = table.Column<int>(type: "int", nullable: false),
                    NumFrames = table.Column<long>(type: "bigint", nullable: false),
                    Format = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    GlobalEmbedding = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true),
                    GlobalEmbeddingDim = table.Column<int>(type: "int", nullable: true),
                    Metadata = table.Column<string>(type: "JSON", nullable: true),
                    IngestionDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Videos", x => x.VideoId);
                });

            migrationBuilder.CreateTable(
                name: "AtomRelations",
                columns: table => new
                {
                    AtomRelationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceAtomId = table.Column<long>(type: "bigint", nullable: false),
                    TargetAtomId = table.Column<long>(type: "bigint", nullable: false),
                    RelationType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Weight = table.Column<float>(type: "real", nullable: true),
                    SpatialExpression = table.Column<Geometry>(type: "geometry", nullable: true),
                    Metadata = table.Column<string>(type: "JSON", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtomRelations", x => x.AtomRelationId);
                    table.ForeignKey(
                        name: "FK_AtomRelations_Atoms_SourceAtomId",
                        column: x => x.SourceAtomId,
                        principalTable: "Atoms",
                        principalColumn: "AtomId");
                    table.ForeignKey(
                        name: "FK_AtomRelations_Atoms_TargetAtomId",
                        column: x => x.TargetAtomId,
                        principalTable: "Atoms",
                        principalColumn: "AtomId");
                });

            migrationBuilder.CreateTable(
                name: "AudioFrames",
                schema: "dbo",
                columns: table => new
                {
                    AudioId = table.Column<long>(type: "bigint", nullable: false),
                    FrameNumber = table.Column<long>(type: "bigint", nullable: false),
                    TimestampMs = table.Column<long>(type: "bigint", nullable: false),
                    AmplitudeL = table.Column<float>(type: "real", nullable: true),
                    AmplitudeR = table.Column<float>(type: "real", nullable: true),
                    SpectralCentroid = table.Column<float>(type: "real", nullable: true),
                    SpectralRolloff = table.Column<float>(type: "real", nullable: true),
                    ZeroCrossingRate = table.Column<float>(type: "real", nullable: true),
                    RmsEnergy = table.Column<float>(type: "real", nullable: true),
                    Mfcc = table.Column<SqlVector<float>>(type: "VECTOR(13)", nullable: true),
                    FrameEmbedding = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioFrames", x => new { x.AudioId, x.FrameNumber });
                    table.ForeignKey(
                        name: "FK_AudioFrames_AudioData_AudioId",
                        column: x => x.AudioId,
                        principalSchema: "dbo",
                        principalTable: "AudioData",
                        principalColumn: "AudioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BillingMultipliers",
                columns: table => new
                {
                    MultiplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    RatePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Dimension = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: ""),
                    Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, defaultValue: ""),
                    Multiplier = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingMultipliers", x => x.MultiplierId);
                    table.ForeignKey(
                        name: "FK_BillingMultipliers_BillingRatePlans_RatePlanId",
                        column: x => x.RatePlanId,
                        principalTable: "BillingRatePlans",
                        principalColumn: "RatePlanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BillingOperationRates",
                columns: table => new
                {
                    OperationRateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    RatePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, defaultValue: ""),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, defaultValue: ""),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Rate = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingOperationRates", x => x.OperationRateId);
                    table.ForeignKey(
                        name: "FK_BillingOperationRates_BillingRatePlans_RatePlanId",
                        column: x => x.RatePlanId,
                        principalTable: "BillingRatePlans",
                        principalColumn: "RatePlanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImagePatches",
                schema: "dbo",
                columns: table => new
                {
                    PatchId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImageId = table.Column<long>(type: "bigint", nullable: false),
                    PatchX = table.Column<int>(type: "int", nullable: false),
                    PatchY = table.Column<int>(type: "int", nullable: false),
                    PatchWidth = table.Column<int>(type: "int", nullable: false),
                    PatchHeight = table.Column<int>(type: "int", nullable: false),
                    PatchRegion = table.Column<Geometry>(type: "GEOMETRY", nullable: false),
                    PatchEmbedding = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true),
                    DominantColor = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    MeanIntensity = table.Column<float>(type: "real", nullable: true),
                    StdIntensity = table.Column<float>(type: "real", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagePatches", x => x.PatchId);
                    table.ForeignKey(
                        name: "FK_ImagePatches_Images_ImageId",
                        column: x => x.ImageId,
                        principalSchema: "dbo",
                        principalTable: "Images",
                        principalColumn: "ImageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IngestionJobAtoms",
                columns: table => new
                {
                    IngestionJobAtomId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IngestionJobId = table.Column<long>(type: "bigint", nullable: false),
                    AtomId = table.Column<long>(type: "bigint", nullable: false),
                    WasDuplicate = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestionJobAtoms", x => x.IngestionJobAtomId);
                    table.ForeignKey(
                        name: "FK_IngestionJobAtoms_Atoms_AtomId",
                        column: x => x.AtomId,
                        principalTable: "Atoms",
                        principalColumn: "AtomId");
                    table.ForeignKey(
                        name: "FK_IngestionJobAtoms_IngestionJobs_IngestionJobId",
                        column: x => x.IngestionJobId,
                        principalTable: "IngestionJobs",
                        principalColumn: "IngestionJobId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AtomEmbeddings",
                columns: table => new
                {
                    AtomEmbeddingId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AtomId = table.Column<long>(type: "bigint", nullable: false),
                    ModelId = table.Column<int>(type: "int", nullable: true),
                    EmbeddingType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Dimension = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    EmbeddingVector = table.Column<SqlVector<float>>(type: "VECTOR(1998)", nullable: true),
                    UsesMaxDimensionPadding = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SpatialProjX = table.Column<double>(type: "float", nullable: true),
                    SpatialProjY = table.Column<double>(type: "float", nullable: true),
                    SpatialProjZ = table.Column<double>(type: "float", nullable: true),
                    SpatialGeometry = table.Column<Point>(type: "geometry", nullable: true),
                    SpatialCoarse = table.Column<Point>(type: "geometry", nullable: true),
                    SpatialBucketX = table.Column<int>(type: "int", nullable: true),
                    SpatialBucketY = table.Column<int>(type: "int", nullable: true),
                    SpatialBucketZ = table.Column<int>(type: "int", nullable: false, defaultValue: int.MinValue),
                    Metadata = table.Column<string>(type: "JSON", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtomEmbeddings", x => x.AtomEmbeddingId);
                    table.ForeignKey(
                        name: "FK_AtomEmbeddings_Atoms_AtomId",
                        column: x => x.AtomId,
                        principalTable: "Atoms",
                        principalColumn: "AtomId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AtomEmbeddings_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "ModelId");
                });

            migrationBuilder.CreateTable(
                name: "AtomEmbeddingSpatialMetadata",
                columns: table => new
                {
                    SpatialBucketX = table.Column<int>(type: "int", nullable: false),
                    SpatialBucketY = table.Column<int>(type: "int", nullable: false),
                    SpatialBucketZ = table.Column<int>(type: "int", nullable: false),
                    HasZ = table.Column<bool>(type: "bit", nullable: false),
                    EmbeddingCount = table.Column<long>(type: "bigint", nullable: false),
                    MinProjX = table.Column<double>(type: "float", nullable: true),
                    MaxProjX = table.Column<double>(type: "float", nullable: true),
                    MinProjY = table.Column<double>(type: "float", nullable: true),
                    MaxProjY = table.Column<double>(type: "float", nullable: true),
                    MinProjZ = table.Column<double>(type: "float", nullable: true),
                    MaxProjZ = table.Column<double>(type: "float", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtomEmbeddingSpatialMetadata", x => new { x.SpatialBucketX, x.SpatialBucketY, x.SpatialBucketZ, x.HasZ });
                });

            migrationBuilder.CreateTable(
                name: "InferenceRequests",
                columns: table => new
                {
                    InferenceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    TaskType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InputData = table.Column<string>(type: "JSON", nullable: true),
                    InputHash = table.Column<byte[]>(type: "binary(32)", maxLength: 32, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Confidence = table.Column<double>(type: "float", nullable: true),
                    ModelsUsed = table.Column<string>(type: "JSON", nullable: true),
                    EnsembleStrategy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OutputData = table.Column<string>(type: "JSON", nullable: true),
                    OutputMetadata = table.Column<string>(type: "JSON", nullable: true),
                    TotalDurationMs = table.Column<int>(type: "int", nullable: true),
                    CacheHit = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    UserRating = table.Column<byte>(type: "tinyint", nullable: true),
                    UserFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModelId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InferenceRequests", x => x.InferenceId);
                    table.ForeignKey(
                        name: "FK_InferenceRequests_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "ModelId");
                });

            migrationBuilder.CreateTable(
                name: "ModelLayers",
                columns: table => new
                {
                    LayerId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelId = table.Column<int>(type: "int", nullable: false),
                    LayerIdx = table.Column<int>(type: "int", nullable: false),
                    LayerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LayerType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WeightsGeometry = table.Column<LineString>(type: "geometry", nullable: true),
                    TensorShape = table.Column<string>(type: "NVARCHAR(200)", nullable: true),
                    TensorDtype = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "float32"),
                    QuantizationType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    QuantizationScale = table.Column<double>(type: "float", nullable: true),
                    QuantizationZeroPoint = table.Column<double>(type: "float", nullable: true),
                    Parameters = table.Column<string>(type: "JSON", nullable: true),
                    ParameterCount = table.Column<long>(type: "bigint", nullable: true),
                    ZMin = table.Column<double>(type: "float", nullable: true),
                    ZMax = table.Column<double>(type: "float", nullable: true),
                    MMin = table.Column<double>(type: "float", nullable: true),
                    MMax = table.Column<double>(type: "float", nullable: true),
                    MortonCode = table.Column<long>(type: "bigint", nullable: true),
                    PreviewPointCount = table.Column<int>(type: "int", nullable: true),
                    CacheHitRate = table.Column<double>(type: "float", nullable: true, defaultValue: 0.0),
                    AvgComputeTimeMs = table.Column<double>(type: "float", nullable: true),
                    LayerAtomId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelLayers", x => x.LayerId);
                    table.ForeignKey(
                        name: "FK_ModelLayers_Atoms_LayerAtomId",
                        column: x => x.LayerAtomId,
                        principalTable: "Atoms",
                        principalColumn: "AtomId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ModelLayers_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "ModelId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModelMetadata",
                columns: table => new
                {
                    MetadataId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelId = table.Column<int>(type: "int", nullable: false),
                    SupportedTasks = table.Column<string>(type: "JSON", nullable: true),
                    SupportedModalities = table.Column<string>(type: "JSON", nullable: true),
                    MaxInputLength = table.Column<int>(type: "int", nullable: true),
                    MaxOutputLength = table.Column<int>(type: "int", nullable: true),
                    EmbeddingDimension = table.Column<int>(type: "int", nullable: true),
                    PerformanceMetrics = table.Column<string>(type: "JSON", nullable: true),
                    TrainingDataset = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TrainingDate = table.Column<DateOnly>(type: "date", nullable: true),
                    License = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SourceUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelMetadata", x => x.MetadataId);
                    table.ForeignKey(
                        name: "FK_ModelMetadata_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "ModelId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TokenVocabulary",
                columns: table => new
                {
                    VocabId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TokenId = table.Column<int>(type: "int", nullable: false),
                    TokenType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Embedding = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true),
                    EmbeddingDim = table.Column<int>(type: "int", nullable: true),
                    Frequency = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    LastUsed = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenVocabulary", x => x.VocabId);
                    table.ForeignKey(
                        name: "FK_TokenVocabulary_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "ModelId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoFrames",
                schema: "dbo",
                columns: table => new
                {
                    FrameId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VideoId = table.Column<long>(type: "bigint", nullable: false),
                    FrameNumber = table.Column<long>(type: "bigint", nullable: false),
                    TimestampMs = table.Column<long>(type: "bigint", nullable: false),
                    PixelCloud = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    ObjectRegions = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    MotionVectors = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    OpticalFlow = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    FrameEmbedding = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true),
                    PerceptualHash = table.Column<byte[]>(type: "varbinary(8)", maxLength: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoFrames", x => x.FrameId);
                    table.ForeignKey(
                        name: "FK_VideoFrames_Videos_VideoId",
                        column: x => x.VideoId,
                        principalSchema: "dbo",
                        principalTable: "Videos",
                        principalColumn: "VideoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AtomEmbeddingComponents",
                columns: table => new
                {
                    AtomEmbeddingComponentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AtomEmbeddingId = table.Column<long>(type: "bigint", nullable: false),
                    ComponentIndex = table.Column<int>(type: "int", nullable: false),
                    ComponentValue = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtomEmbeddingComponents", x => x.AtomEmbeddingComponentId);
                    table.ForeignKey(
                        name: "FK_AtomEmbeddingComponents_AtomEmbeddings_AtomEmbeddingId",
                        column: x => x.AtomEmbeddingId,
                        principalTable: "AtomEmbeddings",
                        principalColumn: "AtomEmbeddingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InferenceSteps",
                columns: table => new
                {
                    StepId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InferenceId = table.Column<long>(type: "bigint", nullable: false),
                    StepNumber = table.Column<int>(type: "int", nullable: false),
                    ModelId = table.Column<int>(type: "int", nullable: true),
                    LayerId = table.Column<long>(type: "bigint", nullable: true),
                    OperationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    QueryText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IndexUsed = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RowsExamined = table.Column<long>(type: "bigint", nullable: true),
                    RowsReturned = table.Column<long>(type: "bigint", nullable: true),
                    DurationMs = table.Column<int>(type: "int", nullable: true),
                    CacheUsed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InferenceSteps", x => x.StepId);
                    table.ForeignKey(
                        name: "FK_InferenceSteps_InferenceRequests_InferenceId",
                        column: x => x.InferenceId,
                        principalTable: "InferenceRequests",
                        principalColumn: "InferenceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InferenceSteps_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "ModelId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CachedActivations",
                columns: table => new
                {
                    CacheId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelId = table.Column<int>(type: "int", nullable: false),
                    LayerId = table.Column<long>(type: "bigint", nullable: false),
                    InputHash = table.Column<byte[]>(type: "binary(32)", maxLength: 32, nullable: false),
                    ActivationOutput = table.Column<SqlVector<float>>(type: "VECTOR(1998)", nullable: true),
                    OutputShape = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HitCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    LastAccessed = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ComputeTimeSavedMs = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedActivations", x => x.CacheId);
                    table.ForeignKey(
                        name: "FK_CachedActivations_ModelLayers_LayerId",
                        column: x => x.LayerId,
                        principalTable: "ModelLayers",
                        principalColumn: "LayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CachedActivations_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "ModelId");
                });

            migrationBuilder.CreateTable(
                name: "LayerTensorSegments",
                columns: table => new
                {
                    LayerTensorSegmentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LayerId = table.Column<long>(type: "bigint", nullable: false),
                    SegmentOrdinal = table.Column<int>(type: "int", nullable: false),
                    PointOffset = table.Column<long>(type: "bigint", nullable: false),
                    PointCount = table.Column<int>(type: "int", nullable: false),
                    QuantizationType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    QuantizationScale = table.Column<double>(type: "float", nullable: true),
                    QuantizationZeroPoint = table.Column<double>(type: "float", nullable: true),
                    ZMin = table.Column<double>(type: "float", nullable: true),
                    ZMax = table.Column<double>(type: "float", nullable: true),
                    MMin = table.Column<double>(type: "float", nullable: true),
                    MMax = table.Column<double>(type: "float", nullable: true),
                    MortonCode = table.Column<long>(type: "bigint", nullable: true),
                    GeometryFootprint = table.Column<Geometry>(type: "geometry", nullable: true),
                    RawPayload = table.Column<byte[]>(type: "VARBINARY(MAX) FILESTREAM", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "DATETIME2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    PayloadRowGuid = table.Column<Guid>(type: "uniqueidentifier ROWGUIDCOL", nullable: false, defaultValueSql: "NEWSEQUENTIALID()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LayerTensorSegments", x => x.LayerTensorSegmentId);
                    table.UniqueConstraint("UX_LayerTensorSegments_PayloadRowGuid", x => x.PayloadRowGuid);
                    table.ForeignKey(
                        name: "FK_LayerTensorSegments_ModelLayers_LayerId",
                        column: x => x.LayerId,
                        principalTable: "ModelLayers",
                        principalColumn: "LayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TensorAtoms",
                columns: table => new
                {
                    TensorAtomId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AtomId = table.Column<long>(type: "bigint", nullable: false),
                    ModelId = table.Column<int>(type: "int", nullable: true),
                    LayerId = table.Column<long>(type: "bigint", nullable: true),
                    AtomType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SpatialSignature = table.Column<Point>(type: "geometry", nullable: true),
                    GeometryFootprint = table.Column<Geometry>(type: "geometry", nullable: true),
                    Metadata = table.Column<string>(type: "JSON", nullable: true),
                    ImportanceScore = table.Column<float>(type: "real", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TensorAtoms", x => x.TensorAtomId);
                    table.ForeignKey(
                        name: "FK_TensorAtoms_Atoms_AtomId",
                        column: x => x.AtomId,
                        principalTable: "Atoms",
                        principalColumn: "AtomId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TensorAtoms_ModelLayers_LayerId",
                        column: x => x.LayerId,
                        principalTable: "ModelLayers",
                        principalColumn: "LayerId");
                    table.ForeignKey(
                        name: "FK_TensorAtoms_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "ModelId");
                });

            migrationBuilder.CreateTable(
                name: "TensorAtomCoefficients",
                columns: table => new
                {
                    TensorAtomCoefficientId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TensorAtomId = table.Column<long>(type: "bigint", nullable: false),
                    ParentLayerId = table.Column<long>(type: "bigint", nullable: false),
                    TensorRole = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Coefficient = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TensorAtomCoefficients", x => x.TensorAtomCoefficientId);
                    table.ForeignKey(
                        name: "FK_TensorAtomCoefficients_ModelLayers_ParentLayerId",
                        column: x => x.ParentLayerId,
                        principalTable: "ModelLayers",
                        principalColumn: "LayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TensorAtomCoefficients_TensorAtoms_TensorAtomId",
                        column: x => x.TensorAtomId,
                        principalTable: "TensorAtoms",
                        principalColumn: "TensorAtomId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_AtomEmbeddingComponents_Embedding_Index",
                table: "AtomEmbeddingComponents",
                columns: new[] { "AtomEmbeddingId", "ComponentIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AtomEmbeddings_Atom_Model_Type",
                table: "AtomEmbeddings",
                columns: new[] { "AtomId", "EmbeddingType", "ModelId" });

            migrationBuilder.CreateIndex(
                name: "IX_AtomEmbeddings_ModelId",
                table: "AtomEmbeddings",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_AtomEmbeddings_SpatialBucket",
                table: "AtomEmbeddings",
                columns: new[] { "SpatialBucketX", "SpatialBucketY", "SpatialBucketZ" });

            migrationBuilder.CreateIndex(
                name: "IX_AtomEmbeddingSpatialMetadata_Count",
                table: "AtomEmbeddingSpatialMetadata",
                column: "EmbeddingCount");

            migrationBuilder.CreateIndex(
                name: "IX_AtomicAudioSamples_AmplitudeNormalized",
                table: "AtomicAudioSamples",
                column: "AmplitudeNormalized");

            migrationBuilder.CreateIndex(
                name: "IX_AtomicTextTokens_TokenHash",
                table: "AtomicTextTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AtomicTextTokens_TokenText",
                table: "AtomicTextTokens",
                column: "TokenText",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AtomRelations_Source_Target_Type",
                table: "AtomRelations",
                columns: new[] { "SourceAtomId", "TargetAtomId", "RelationType" });

            migrationBuilder.CreateIndex(
                name: "IX_AtomRelations_TargetAtomId",
                table: "AtomRelations",
                column: "TargetAtomId");

            migrationBuilder.CreateIndex(
                name: "UX_Atoms_ContentHash",
                table: "Atoms",
                column: "ContentHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AudioData_DurationMs",
                schema: "dbo",
                table: "AudioData",
                column: "DurationMs");

            migrationBuilder.CreateIndex(
                name: "IX_AudioData_IngestionDate",
                schema: "dbo",
                table: "AudioData",
                column: "IngestionDate",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "UX_BillingMultipliers_Active",
                table: "BillingMultipliers",
                columns: new[] { "RatePlanId", "Dimension", "Key" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_BillingOperationRates_Active",
                table: "BillingOperationRates",
                columns: new[] { "RatePlanId", "Operation" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_BillingRatePlans_Tenant_IsActive",
                table: "BillingRatePlans",
                columns: new[] { "TenantId", "IsActive" })
                .Annotation("SqlServer:Include", new[] { "UpdatedUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_BillingRatePlans_Tenant_PlanCode",
                table: "BillingRatePlans",
                columns: new[] { "TenantId", "PlanCode" },
                unique: true,
                filter: "[PlanCode] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_CachedActivations_LastAccessed_HitCount",
                table: "CachedActivations",
                columns: new[] { "LastAccessed", "HitCount" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_CachedActivations_LayerId",
                table: "CachedActivations",
                column: "LayerId");

            migrationBuilder.CreateIndex(
                name: "IX_CachedActivations_Model_Layer_InputHash",
                table: "CachedActivations",
                columns: new[] { "ModelId", "LayerId", "InputHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodeAtoms_CodeHash",
                schema: "dbo",
                table: "CodeAtoms",
                column: "CodeHash",
                unique: true,
                filter: "[CodeHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CodeAtoms_CodeType",
                schema: "dbo",
                table: "CodeAtoms",
                column: "CodeType");

            migrationBuilder.CreateIndex(
                name: "IX_CodeAtoms_CreatedAt",
                schema: "dbo",
                table: "CodeAtoms",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CodeAtoms_Language",
                schema: "dbo",
                table: "CodeAtoms",
                column: "Language");

            migrationBuilder.CreateIndex(
                name: "IX_CodeAtoms_QualityScore",
                schema: "dbo",
                table: "CodeAtoms",
                column: "QualityScore");

            migrationBuilder.CreateIndex(
                name: "UX_DeduplicationPolicies_PolicyName",
                table: "DeduplicationPolicies",
                column: "PolicyName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Embeddings_Production_ContentHash",
                table: "Embeddings_Production",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationStreams_Model",
                schema: "provenance",
                table: "GenerationStreams",
                column: "Model");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationStreams_Scope",
                schema: "provenance",
                table: "GenerationStreams",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_ImagePatches_ImageId_PatchX_PatchY",
                schema: "dbo",
                table: "ImagePatches",
                columns: new[] { "ImageId", "PatchX", "PatchY" });

            migrationBuilder.CreateIndex(
                name: "IX_Images_IngestionDate",
                schema: "dbo",
                table: "Images",
                column: "IngestionDate",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Images_Width_Height",
                schema: "dbo",
                table: "Images",
                columns: new[] { "Width", "Height" });

            migrationBuilder.CreateIndex(
                name: "IX_InferenceRequests_CacheHit",
                table: "InferenceRequests",
                column: "CacheHit");

            migrationBuilder.CreateIndex(
                name: "IX_InferenceRequests_InputHash",
                table: "InferenceRequests",
                column: "InputHash");

            migrationBuilder.CreateIndex(
                name: "IX_InferenceRequests_ModelId",
                table: "InferenceRequests",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_InferenceRequests_RequestTimestamp",
                table: "InferenceRequests",
                column: "RequestTimestamp",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_InferenceRequests_TaskType",
                table: "InferenceRequests",
                column: "TaskType");

            migrationBuilder.CreateIndex(
                name: "IX_InferenceSteps_InferenceId_StepNumber",
                table: "InferenceSteps",
                columns: new[] { "InferenceId", "StepNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_InferenceSteps_ModelId",
                table: "InferenceSteps",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionJobAtoms_AtomId",
                table: "IngestionJobAtoms",
                column: "AtomId");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionJobAtoms_Job_Atom",
                table: "IngestionJobAtoms",
                columns: new[] { "IngestionJobId", "AtomId" });

            migrationBuilder.CreateIndex(
                name: "IX_LayerTensorSegments_M_Range",
                table: "LayerTensorSegments",
                columns: new[] { "LayerId", "MMin", "MMax" });

            migrationBuilder.CreateIndex(
                name: "IX_LayerTensorSegments_Morton",
                table: "LayerTensorSegments",
                column: "MortonCode");

            migrationBuilder.CreateIndex(
                name: "IX_LayerTensorSegments_Z_Range",
                table: "LayerTensorSegments",
                columns: new[] { "LayerId", "ZMin", "ZMax" });

            migrationBuilder.CreateIndex(
                name: "UX_LayerTensorSegments_LayerId_SegmentOrdinal",
                table: "LayerTensorSegments",
                columns: new[] { "LayerId", "SegmentOrdinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModelLayers_LayerAtomId",
                table: "ModelLayers",
                column: "LayerAtomId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelLayers_LayerType",
                table: "ModelLayers",
                column: "LayerType");

            migrationBuilder.CreateIndex(
                name: "IX_ModelLayers_M_Range",
                table: "ModelLayers",
                columns: new[] { "ModelId", "MMin", "MMax" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelLayers_ModelId_LayerIdx",
                table: "ModelLayers",
                columns: new[] { "ModelId", "LayerIdx" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelLayers_Morton",
                table: "ModelLayers",
                column: "MortonCode");

            migrationBuilder.CreateIndex(
                name: "IX_ModelLayers_Z_Range",
                table: "ModelLayers",
                columns: new[] { "ModelId", "ZMin", "ZMax" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelMetadata_ModelId",
                table: "ModelMetadata",
                column: "ModelId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Models_ModelName",
                table: "Models",
                column: "ModelName");

            migrationBuilder.CreateIndex(
                name: "IX_Models_ModelType",
                table: "Models",
                column: "ModelType");

            migrationBuilder.CreateIndex(
                name: "IX_TensorAtomCoefficients_Lookup",
                table: "TensorAtomCoefficients",
                columns: new[] { "TensorAtomId", "ParentLayerId", "TensorRole" });

            migrationBuilder.CreateIndex(
                name: "IX_TensorAtomCoefficients_ParentLayerId",
                table: "TensorAtomCoefficients",
                column: "ParentLayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TensorAtoms_AtomId",
                table: "TensorAtoms",
                column: "AtomId");

            migrationBuilder.CreateIndex(
                name: "IX_TensorAtoms_LayerId",
                table: "TensorAtoms",
                column: "LayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TensorAtoms_Model_Layer_Type",
                table: "TensorAtoms",
                columns: new[] { "ModelId", "LayerId", "AtomType" });

            migrationBuilder.CreateIndex(
                name: "IX_TokenVocabulary_ModelId_Token",
                table: "TokenVocabulary",
                columns: new[] { "ModelId", "Token" });

            migrationBuilder.CreateIndex(
                name: "IX_TokenVocabulary_ModelId_TokenId",
                table: "TokenVocabulary",
                columns: new[] { "ModelId", "TokenId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoFrames_VideoId_FrameNumber",
                schema: "dbo",
                table: "VideoFrames",
                columns: new[] { "VideoId", "FrameNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoFrames_VideoId_TimestampMs",
                schema: "dbo",
                table: "VideoFrames",
                columns: new[] { "VideoId", "TimestampMs" });

            migrationBuilder.CreateIndex(
                name: "IX_Videos_IngestionDate",
                schema: "dbo",
                table: "Videos",
                column: "IngestionDate",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Videos_ResolutionWidth_ResolutionHeight",
                schema: "dbo",
                table: "Videos",
                columns: new[] { "ResolutionWidth", "ResolutionHeight" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AtomEmbeddingComponents");

            migrationBuilder.DropTable(
                name: "AtomEmbeddingSpatialMetadata");

            migrationBuilder.DropTable(
                name: "AtomicAudioSamples");

            migrationBuilder.DropTable(
                name: "AtomicPixels");

            migrationBuilder.DropTable(
                name: "AtomicTextTokens");

            migrationBuilder.DropTable(
                name: "AtomRelations");

            migrationBuilder.DropTable(
                name: "AudioFrames",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "BillingMultipliers");

            migrationBuilder.DropTable(
                name: "BillingOperationRates");

            migrationBuilder.DropTable(
                name: "CachedActivations");

            migrationBuilder.DropTable(
                name: "CodeAtoms",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "DeduplicationPolicies");

            migrationBuilder.DropTable(
                name: "Embeddings_Production");

            migrationBuilder.DropTable(
                name: "GenerationStreams",
                schema: "provenance");

            migrationBuilder.DropTable(
                name: "ImagePatches",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "InferenceSteps");

            migrationBuilder.DropTable(
                name: "IngestionJobAtoms");

            migrationBuilder.DropTable(
                name: "LayerTensorSegments");

            migrationBuilder.DropTable(
                name: "ModelMetadata");

            migrationBuilder.DropTable(
                name: "TensorAtomCoefficients");

            migrationBuilder.DropTable(
                name: "TextDocuments",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TokenVocabulary");

            migrationBuilder.DropTable(
                name: "VideoFrames",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AtomEmbeddings");

            migrationBuilder.DropTable(
                name: "AudioData",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "BillingRatePlans");

            migrationBuilder.DropTable(
                name: "Images",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "InferenceRequests");

            migrationBuilder.DropTable(
                name: "IngestionJobs");

            migrationBuilder.DropTable(
                name: "TensorAtoms");

            migrationBuilder.DropTable(
                name: "Videos",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ModelLayers");

            migrationBuilder.DropTable(
                name: "Atoms");

            migrationBuilder.DropTable(
                name: "Models");
        }
    }
}

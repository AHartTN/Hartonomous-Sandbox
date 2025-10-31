using System;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Hartonomous.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "AtomicAudioSamples",
                schema: "dbo",
                columns: table => new
                {
                    SampleHash = table.Column<byte[]>(type: "BINARY(32)", nullable: false),
                    AmplitudeNormalized = table.Column<double>(type: "FLOAT", nullable: false),
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
                schema: "dbo",
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
                schema: "dbo",
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
                schema: "dbo",
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
                    SpatialKey = table.Column<Point>(type: "geometry", nullable: true)
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
                    audio_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    source_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    raw_data = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    sample_rate = table.Column<int>(type: "int", nullable: false),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false),
                    num_channels = table.Column<byte>(type: "tinyint", nullable: false),
                    format = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    spectrogram = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    mel_spectrogram = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    waveform_left = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    waveform_right = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    global_embedding = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true),
                    global_embedding_dim = table.Column<int>(type: "int", nullable: true),
                    metadata = table.Column<string>(type: "JSON", nullable: true),
                    ingestion_date = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioData", x => x.audio_id);
                });

            migrationBuilder.CreateTable(
                name: "DeduplicationPolicies",
                schema: "dbo",
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
                schema: "dbo",
                columns: table => new
                {
                    EmbeddingId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    embedding_full = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true),
                    EmbeddingModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SpatialProjX = table.Column<double>(type: "float", nullable: true),
                    SpatialProjY = table.Column<double>(type: "float", nullable: true),
                    SpatialProjZ = table.Column<double>(type: "float", nullable: true),
                    spatial_geometry = table.Column<Point>(type: "geometry", nullable: true),
                    spatial_coarse = table.Column<Point>(type: "geometry", nullable: true),
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
                name: "SpatialAnchors",
                schema: "dbo",
                columns: table => new
                {
                    AnchorId = table.Column<int>(type: "int", nullable: false),
                    AnchorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AnchorVector = table.Column<SqlVector<float>>(type: "VECTOR(1998)", nullable: false),
                    SelectionMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpatialAnchors", x => x.AnchorId);
                });

            migrationBuilder.CreateTable(
                name: "TokenEmbeddingsGeo",
                schema: "dbo",
                columns: table => new
                {
                    TokenId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TokenText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EmbeddingVector = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true),
                    SpatialProjection = table.Column<Point>(type: "geometry", nullable: true),
                    CoarseSpatial = table.Column<Point>(type: "geometry", nullable: true),
                    FineSpatial = table.Column<Point>(type: "geometry", nullable: true),
                    Frequency = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastUsed = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenEmbeddingsGeo", x => x.TokenId);
                });

            migrationBuilder.CreateTable(
                name: "Images",
                schema: "dbo",
                columns: table => new
                {
                    image_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    source_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    source_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    raw_data = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    width = table.Column<int>(type: "int", nullable: false),
                    height = table.Column<int>(type: "int", nullable: false),
                    channels = table.Column<int>(type: "int", nullable: false),
                    format = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    pixel_cloud = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    edge_map = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    object_regions = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    saliency_regions = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    global_embedding = table.Column<SqlVector<float>>(type: "VECTOR(1536)", nullable: true),
                    global_embedding_dim = table.Column<int>(type: "int", nullable: true),
                    metadata = table.Column<string>(type: "JSON", nullable: true),
                    ingestion_date = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "SYSUTCDATETIME()"),
                    last_accessed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    access_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.image_id);
                });

            migrationBuilder.CreateTable(
                name: "IngestionJobs",
                schema: "dbo",
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
                schema: "dbo",
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
                    doc_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    source_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    source_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    raw_text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    char_count = table.Column<int>(type: "int", nullable: true),
                    word_count = table.Column<int>(type: "int", nullable: true),
                    global_embedding = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true),
                    global_embedding_dim = table.Column<int>(type: "int", nullable: true),
                    topic_vector = table.Column<SqlVector<float>>(type: "VECTOR(100)", nullable: true),
                    sentiment_score = table.Column<float>(type: "real", nullable: true),
                    toxicity = table.Column<float>(type: "real", nullable: true),
                    metadata = table.Column<string>(type: "JSON", nullable: true),
                    ingestion_date = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "SYSUTCDATETIME()"),
                    last_accessed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    access_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TextDocuments", x => x.doc_id);
                });

            migrationBuilder.CreateTable(
                name: "Videos",
                schema: "dbo",
                columns: table => new
                {
                    video_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    source_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    raw_data = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    fps = table.Column<int>(type: "int", nullable: false),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false),
                    resolution_width = table.Column<int>(type: "int", nullable: false),
                    resolution_height = table.Column<int>(type: "int", nullable: false),
                    num_frames = table.Column<long>(type: "bigint", nullable: false),
                    format = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    global_embedding = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true),
                    global_embedding_dim = table.Column<int>(type: "int", nullable: true),
                    metadata = table.Column<string>(type: "JSON", nullable: true),
                    ingestion_date = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Videos", x => x.video_id);
                });

            migrationBuilder.CreateTable(
                name: "AtomRelations",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "Atoms",
                        principalColumn: "AtomId");
                    table.ForeignKey(
                        name: "FK_AtomRelations_Atoms_TargetAtomId",
                        column: x => x.TargetAtomId,
                        principalSchema: "dbo",
                        principalTable: "Atoms",
                        principalColumn: "AtomId");
                });

            migrationBuilder.CreateTable(
                name: "AudioFrames",
                schema: "dbo",
                columns: table => new
                {
                    audio_id = table.Column<long>(type: "bigint", nullable: false),
                    frame_number = table.Column<long>(type: "bigint", nullable: false),
                    timestamp_ms = table.Column<long>(type: "bigint", nullable: false),
                    amplitude_l = table.Column<float>(type: "real", nullable: true),
                    amplitude_r = table.Column<float>(type: "real", nullable: true),
                    spectral_centroid = table.Column<float>(type: "real", nullable: true),
                    spectral_rolloff = table.Column<float>(type: "real", nullable: true),
                    zero_crossing_rate = table.Column<float>(type: "real", nullable: true),
                    rms_energy = table.Column<float>(type: "real", nullable: true),
                    mfcc = table.Column<SqlVector<float>>(type: "VECTOR(13)", nullable: true),
                    frame_embedding = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioFrames", x => new { x.audio_id, x.frame_number });
                    table.ForeignKey(
                        name: "FK_AudioFrames_AudioData_audio_id",
                        column: x => x.audio_id,
                        principalSchema: "dbo",
                        principalTable: "AudioData",
                        principalColumn: "audio_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImagePatches",
                schema: "dbo",
                columns: table => new
                {
                    patch_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    image_id = table.Column<long>(type: "bigint", nullable: false),
                    patch_x = table.Column<int>(type: "int", nullable: false),
                    patch_y = table.Column<int>(type: "int", nullable: false),
                    patch_width = table.Column<int>(type: "int", nullable: false),
                    patch_height = table.Column<int>(type: "int", nullable: false),
                    patch_region = table.Column<Geometry>(type: "GEOMETRY", nullable: false),
                    patch_embedding = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true),
                    dominant_color = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    texture_features = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    mean_intensity = table.Column<float>(type: "real", nullable: true),
                    std_intensity = table.Column<float>(type: "real", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagePatches", x => x.patch_id);
                    table.ForeignKey(
                        name: "FK_ImagePatches_Images_image_id",
                        column: x => x.image_id,
                        principalSchema: "dbo",
                        principalTable: "Images",
                        principalColumn: "image_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IngestionJobAtoms",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "Atoms",
                        principalColumn: "AtomId");
                    table.ForeignKey(
                        name: "FK_IngestionJobAtoms_IngestionJobs_IngestionJobId",
                        column: x => x.IngestionJobId,
                        principalSchema: "dbo",
                        principalTable: "IngestionJobs",
                        principalColumn: "IngestionJobId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AtomEmbeddings",
                schema: "dbo",
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
                    SpatialGeometry = table.Column<Point>(type: "geometry", nullable: true),
                    SpatialCoarse = table.Column<Point>(type: "geometry", nullable: true),
                    Metadata = table.Column<string>(type: "JSON", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtomEmbeddings", x => x.AtomEmbeddingId);
                    table.ForeignKey(
                        name: "FK_AtomEmbeddings_Atoms_AtomId",
                        column: x => x.AtomId,
                        principalSchema: "dbo",
                        principalTable: "Atoms",
                        principalColumn: "AtomId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AtomEmbeddings_Models_ModelId",
                        column: x => x.ModelId,
                        principalSchema: "dbo",
                        principalTable: "Models",
                        principalColumn: "ModelId");
                });

            migrationBuilder.CreateTable(
                name: "InferenceRequests",
                schema: "dbo",
                columns: table => new
                {
                    InferenceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    TaskType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InputData = table.Column<string>(type: "JSON", nullable: true),
                    InputHash = table.Column<byte[]>(type: "binary(32)", maxLength: 32, nullable: true),
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
                        principalSchema: "dbo",
                        principalTable: "Models",
                        principalColumn: "ModelId");
                });

            migrationBuilder.CreateTable(
                name: "ModelLayers",
                schema: "dbo",
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
                    CacheHitRate = table.Column<double>(type: "float", nullable: true, defaultValue: 0.0),
                    AvgComputeTimeMs = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelLayers", x => x.LayerId);
                    table.ForeignKey(
                        name: "FK_ModelLayers_Models_ModelId",
                        column: x => x.ModelId,
                        principalSchema: "dbo",
                        principalTable: "Models",
                        principalColumn: "ModelId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModelMetadata",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "Models",
                        principalColumn: "ModelId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TokenVocabulary",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "Models",
                        principalColumn: "ModelId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoFrames",
                schema: "dbo",
                columns: table => new
                {
                    frame_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    video_id = table.Column<long>(type: "bigint", nullable: false),
                    frame_number = table.Column<long>(type: "bigint", nullable: false),
                    timestamp_ms = table.Column<long>(type: "bigint", nullable: false),
                    pixel_cloud = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    object_regions = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    motion_vectors = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    optical_flow = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    frame_embedding = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true),
                    perceptual_hash = table.Column<byte[]>(type: "varbinary(8)", maxLength: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoFrames", x => x.frame_id);
                    table.ForeignKey(
                        name: "FK_VideoFrames_Videos_video_id",
                        column: x => x.video_id,
                        principalSchema: "dbo",
                        principalTable: "Videos",
                        principalColumn: "video_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AtomEmbeddingComponents",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "AtomEmbeddings",
                        principalColumn: "AtomEmbeddingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InferenceSteps",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "InferenceRequests",
                        principalColumn: "InferenceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InferenceSteps_Models_ModelId",
                        column: x => x.ModelId,
                        principalSchema: "dbo",
                        principalTable: "Models",
                        principalColumn: "ModelId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CachedActivations",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "ModelLayers",
                        principalColumn: "LayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CachedActivations_Models_ModelId",
                        column: x => x.ModelId,
                        principalSchema: "dbo",
                        principalTable: "Models",
                        principalColumn: "ModelId");
                });

            migrationBuilder.CreateTable(
                name: "TensorAtoms",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "Atoms",
                        principalColumn: "AtomId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TensorAtoms_ModelLayers_LayerId",
                        column: x => x.LayerId,
                        principalSchema: "dbo",
                        principalTable: "ModelLayers",
                        principalColumn: "LayerId");
                    table.ForeignKey(
                        name: "FK_TensorAtoms_Models_ModelId",
                        column: x => x.ModelId,
                        principalSchema: "dbo",
                        principalTable: "Models",
                        principalColumn: "ModelId");
                });

            migrationBuilder.CreateTable(
                name: "TensorAtomCoefficients",
                schema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "ModelLayers",
                        principalColumn: "LayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TensorAtomCoefficients_TensorAtoms_TensorAtomId",
                        column: x => x.TensorAtomId,
                        principalSchema: "dbo",
                        principalTable: "TensorAtoms",
                        principalColumn: "TensorAtomId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_AtomEmbeddingComponents_Embedding_Index",
                schema: "dbo",
                table: "AtomEmbeddingComponents",
                columns: new[] { "AtomEmbeddingId", "ComponentIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AtomEmbeddings_Atom_Model_Type",
                schema: "dbo",
                table: "AtomEmbeddings",
                columns: new[] { "AtomId", "EmbeddingType", "ModelId" });

            migrationBuilder.CreateIndex(
                name: "IX_AtomEmbeddings_ModelId",
                schema: "dbo",
                table: "AtomEmbeddings",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "idx_amplitude",
                schema: "dbo",
                table: "AtomicAudioSamples",
                column: "AmplitudeNormalized");

            migrationBuilder.CreateIndex(
                name: "idx_token_text",
                schema: "dbo",
                table: "AtomicTextTokens",
                column: "TokenText",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AtomicTextTokens_TokenHash",
                schema: "dbo",
                table: "AtomicTextTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AtomRelations_Source_Target_Type",
                schema: "dbo",
                table: "AtomRelations",
                columns: new[] { "SourceAtomId", "TargetAtomId", "RelationType" });

            migrationBuilder.CreateIndex(
                name: "IX_AtomRelations_TargetAtomId",
                schema: "dbo",
                table: "AtomRelations",
                column: "TargetAtomId");

            migrationBuilder.CreateIndex(
                name: "UX_Atoms_ContentHash",
                schema: "dbo",
                table: "Atoms",
                column: "ContentHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_duration",
                schema: "dbo",
                table: "AudioData",
                column: "duration_ms");

            migrationBuilder.CreateIndex(
                name: "idx_ingestion",
                schema: "dbo",
                table: "AudioData",
                column: "ingestion_date",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_cache_lookup",
                schema: "dbo",
                table: "CachedActivations",
                columns: new[] { "ModelId", "LayerId", "InputHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_cache_usage",
                schema: "dbo",
                table: "CachedActivations",
                columns: new[] { "LastAccessed", "HitCount" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_CachedActivations_LayerId",
                schema: "dbo",
                table: "CachedActivations",
                column: "LayerId");

            migrationBuilder.CreateIndex(
                name: "UX_DeduplicationPolicies_PolicyName",
                schema: "dbo",
                table: "DeduplicationPolicies",
                column: "PolicyName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_content_hash",
                schema: "dbo",
                table: "Embeddings_Production",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "idx_image_patches",
                schema: "dbo",
                table: "ImagePatches",
                columns: new[] { "image_id", "patch_x", "patch_y" });

            migrationBuilder.CreateIndex(
                name: "idx_dimensions",
                schema: "dbo",
                table: "Images",
                columns: new[] { "width", "height" });

            migrationBuilder.CreateIndex(
                name: "idx_ingestion",
                schema: "dbo",
                table: "Images",
                column: "ingestion_date",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_cache_hit",
                schema: "dbo",
                table: "InferenceRequests",
                column: "CacheHit");

            migrationBuilder.CreateIndex(
                name: "idx_input_hash",
                schema: "dbo",
                table: "InferenceRequests",
                column: "InputHash");

            migrationBuilder.CreateIndex(
                name: "idx_task_type",
                schema: "dbo",
                table: "InferenceRequests",
                column: "TaskType");

            migrationBuilder.CreateIndex(
                name: "idx_timestamp",
                schema: "dbo",
                table: "InferenceRequests",
                column: "RequestTimestamp",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_InferenceRequests_ModelId",
                schema: "dbo",
                table: "InferenceRequests",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "idx_inference_steps",
                schema: "dbo",
                table: "InferenceSteps",
                columns: new[] { "InferenceId", "StepNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_InferenceSteps_ModelId",
                schema: "dbo",
                table: "InferenceSteps",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionJobAtoms_AtomId",
                schema: "dbo",
                table: "IngestionJobAtoms",
                column: "AtomId");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionJobAtoms_Job_Atom",
                schema: "dbo",
                table: "IngestionJobAtoms",
                columns: new[] { "IngestionJobId", "AtomId" });

            migrationBuilder.CreateIndex(
                name: "idx_layer_type",
                schema: "dbo",
                table: "ModelLayers",
                column: "LayerType");

            migrationBuilder.CreateIndex(
                name: "idx_model_layer",
                schema: "dbo",
                table: "ModelLayers",
                columns: new[] { "ModelId", "LayerIdx" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelMetadata_ModelId",
                schema: "dbo",
                table: "ModelMetadata",
                column: "ModelId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_model_name",
                schema: "dbo",
                table: "Models",
                column: "ModelName");

            migrationBuilder.CreateIndex(
                name: "idx_model_type",
                schema: "dbo",
                table: "Models",
                column: "ModelType");

            migrationBuilder.CreateIndex(
                name: "IX_TensorAtomCoefficients_Lookup",
                schema: "dbo",
                table: "TensorAtomCoefficients",
                columns: new[] { "TensorAtomId", "ParentLayerId", "TensorRole" });

            migrationBuilder.CreateIndex(
                name: "IX_TensorAtomCoefficients_ParentLayerId",
                schema: "dbo",
                table: "TensorAtomCoefficients",
                column: "ParentLayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TensorAtoms_AtomId",
                schema: "dbo",
                table: "TensorAtoms",
                column: "AtomId");

            migrationBuilder.CreateIndex(
                name: "IX_TensorAtoms_LayerId",
                schema: "dbo",
                table: "TensorAtoms",
                column: "LayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TensorAtoms_Model_Layer_Type",
                schema: "dbo",
                table: "TensorAtoms",
                columns: new[] { "ModelId", "LayerId", "AtomType" });

            migrationBuilder.CreateIndex(
                name: "idx_model_token",
                schema: "dbo",
                table: "TokenVocabulary",
                columns: new[] { "ModelId", "TokenId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_token_text",
                schema: "dbo",
                table: "TokenVocabulary",
                columns: new[] { "ModelId", "Token" });

            migrationBuilder.CreateIndex(
                name: "idx_timestamp",
                schema: "dbo",
                table: "VideoFrames",
                columns: new[] { "video_id", "timestamp_ms" });

            migrationBuilder.CreateIndex(
                name: "idx_video_frame",
                schema: "dbo",
                table: "VideoFrames",
                columns: new[] { "video_id", "frame_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_ingestion",
                schema: "dbo",
                table: "Videos",
                column: "ingestion_date",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_resolution",
                schema: "dbo",
                table: "Videos",
                columns: new[] { "resolution_width", "resolution_height" });

            migrationBuilder.CreateIndex(
                name: "IX_TokenEmbeddingsGeo_TokenText",
                schema: "dbo",
                table: "TokenEmbeddingsGeo",
                column: "TokenText");

            // Spatial indexes for hybrid vector search and atom substrate acceleration
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'idx_spatial_embedding'
                      AND object_id = OBJECT_ID('dbo.TokenEmbeddingsGeo'))
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
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_spatial_coarse' AND object_id = OBJECT_ID('dbo.Embeddings_Production'))
                BEGIN
                    CREATE SPATIAL INDEX idx_spatial_coarse ON dbo.Embeddings_Production(spatial_coarse)
                    WITH (
                        BOUNDING_BOX = (-10, -10, 10, 10),
                        GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = LOW, LEVEL_3 = LOW, LEVEL_4 = LOW),
                        CELLS_PER_OBJECT = 8
                    );
                END;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_spatial_fine' AND object_id = OBJECT_ID('dbo.Embeddings_Production'))
                BEGIN
                    CREATE SPATIAL INDEX idx_spatial_fine ON dbo.Embeddings_Production(spatial_geometry)
                    WITH (
                        BOUNDING_BOX = (-10, -10, 10, 10),
                        GRIDS = (LEVEL_1 = HIGH, LEVEL_2 = HIGH, LEVEL_3 = MEDIUM, LEVEL_4 = LOW),
                        CELLS_PER_OBJECT = 16
                    );
                END;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_color_space' AND object_id = OBJECT_ID('dbo.AtomicPixels'))
                BEGIN
                    CREATE SPATIAL INDEX idx_color_space ON dbo.AtomicPixels(ColorPoint)
                    WITH (
                        BOUNDING_BOX = (0, 0, 255, 255),
                        GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM),
                        CELLS_PER_OBJECT = 16
                    );
                END;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_weights_spatial' AND object_id = OBJECT_ID('dbo.ModelLayers'))
                BEGIN
                    CREATE SPATIAL INDEX idx_weights_spatial ON dbo.ModelLayers(WeightsGeometry)
                    WITH (
                        BOUNDING_BOX = (-10, -10, 10, 10),
                        GRIDS = (MEDIUM, MEDIUM, MEDIUM, MEDIUM),
                        CELLS_PER_OBJECT = 16
                    );
                END;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM dbo.TokenEmbeddingsGeo)
                BEGIN
                    INSERT INTO dbo.TokenEmbeddingsGeo (TokenText, SpatialProjection, CoarseSpatial, FineSpatial, Frequency)
                    VALUES
                        ('the', geometry::STGeomFromText('POINT(0.1 0.2 0.1)', 0), geometry::STGeomFromText('POINT(0 0 0)', 0), geometry::STGeomFromText('POINT(0.1 0.2 0.1)', 0), 512),
                        ('is', geometry::STGeomFromText('POINT(0.15 0.18 0.12)', 0), geometry::STGeomFromText('POINT(0 0 0)', 0), geometry::STGeomFromText('POINT(0.15 0.18 0.12)', 0), 384),
                        ('machine', geometry::STGeomFromText('POINT(5.2 3.1 1.8)', 0), geometry::STGeomFromText('POINT(5 3 2)', 0), geometry::STGeomFromText('POINT(5.2 3.1 1.8)', 0), 120),
                        ('learning', geometry::STGeomFromText('POINT(5.5 3.3 2.1)', 0), geometry::STGeomFromText('POINT(5 3 2)', 0), geometry::STGeomFromText('POINT(5.5 3.3 2.1)', 0), 110),
                        ('database', geometry::STGeomFromText('POINT(-3.1 4.2 -1.5)', 0), geometry::STGeomFromText('POINT(-3 4 -2)', 0), geometry::STGeomFromText('POINT(-3.1 4.2 -1.5)', 0), 95),
                        ('query', geometry::STGeomFromText('POINT(-2.8 4.5 -1.3)', 0), geometry::STGeomFromText('POINT(-3 4 -2)', 0), geometry::STGeomFromText('POINT(-2.8 4.5 -1.3)', 0), 88),
                        ('neural', geometry::STGeomFromText('POINT(5.8 2.9 2.3)', 0), geometry::STGeomFromText('POINT(5 3 2)', 0), geometry::STGeomFromText('POINT(5.8 2.9 2.3)', 0), 140),
                        ('network', geometry::STGeomFromText('POINT(6.1 3.2 2.5)', 0), geometry::STGeomFromText('POINT(5 3 2)', 0), geometry::STGeomFromText('POINT(6.1 3.2 2.5)', 0), 135);
                END;
            ");

            // Stored procedures required by the atom substrate and spatial inference pipeline
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE dbo.sp_ExactVectorSearch
                    @query_vector VECTOR(1998),
                    @top_k INT = 10,
                    @distance_metric NVARCHAR(20) = 'cosine',
                    @embedding_type NVARCHAR(128) = NULL,
                    @model_id INT = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT TOP (@top_k)
                        ae.AtomEmbeddingId,
                        ae.AtomId,
                        a.Modality,
                        a.Subtype,
                        a.SourceUri,
                        a.SourceType,
                        a.CanonicalText,
                        ae.EmbeddingType,
                        ae.ModelId,
                        ae.Dimension,
                        VECTOR_DISTANCE(@distance_metric, ae.EmbeddingVector, @query_vector) AS distance,
                        1.0 - VECTOR_DISTANCE(@distance_metric, ae.EmbeddingVector, @query_vector) AS similarity,
                        ae.CreatedAt
                    FROM dbo.AtomEmbeddings AS ae
                    INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
                    WHERE ae.EmbeddingVector IS NOT NULL
                      AND (@embedding_type IS NULL OR ae.EmbeddingType = @embedding_type)
                      AND (@model_id IS NULL OR ae.ModelId = @model_id)
                    ORDER BY VECTOR_DISTANCE(@distance_metric, ae.EmbeddingVector, @query_vector);
                END;
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE dbo.sp_ApproxSpatialSearch
                    @query_x FLOAT,
                    @query_y FLOAT,
                    @query_z FLOAT,
                    @top_k INT = 10,
                    @use_coarse BIT = 0,
                    @embedding_type NVARCHAR(128) = NULL,
                    @model_id INT = NULL,
                    @srid INT = 0
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @wkt NVARCHAR(200) = CONCAT('POINT (', @query_x, ' ', @query_y, ' ', @query_z, ')');
                    DECLARE @query_point GEOMETRY = geometry::STGeomFromText(@wkt, @srid);

                    IF @use_coarse = 1
                    BEGIN
                        SELECT TOP (@top_k)
                            ae.AtomEmbeddingId,
                            ae.AtomId,
                            a.Modality,
                            a.Subtype,
                            a.SourceUri,
                            a.SourceType,
                            ae.EmbeddingType,
                            ae.ModelId,
                            ae.SpatialCoarse.STDistance(@query_point) AS spatial_distance
                        FROM dbo.AtomEmbeddings AS ae
                        INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
                        WHERE ae.SpatialCoarse IS NOT NULL
                          AND (@embedding_type IS NULL OR ae.EmbeddingType = @embedding_type)
                          AND (@model_id IS NULL OR ae.ModelId = @model_id)
                        ORDER BY ae.SpatialCoarse.STDistance(@query_point);
                    END
                    ELSE
                    BEGIN
                        SELECT TOP (@top_k)
                            ae.AtomEmbeddingId,
                            ae.AtomId,
                            a.Modality,
                            a.Subtype,
                            a.SourceUri,
                            a.SourceType,
                            ae.EmbeddingType,
                            ae.ModelId,
                            ae.SpatialGeometry.STDistance(@query_point) AS spatial_distance
                        FROM dbo.AtomEmbeddings AS ae
                        INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
                        WHERE ae.SpatialGeometry IS NOT NULL
                          AND (@embedding_type IS NULL OR ae.EmbeddingType = @embedding_type)
                          AND (@model_id IS NULL OR ae.ModelId = @model_id)
                        ORDER BY ae.SpatialGeometry.STDistance(@query_point);
                    END;
                END;
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE dbo.sp_HybridSearch
                    @query_vector VECTOR(1998),
                    @query_spatial_x FLOAT,
                    @query_spatial_y FLOAT,
                    @query_spatial_z FLOAT,
                    @spatial_candidates INT = 100,
                    @final_top_k INT = 10,
                    @distance_metric NVARCHAR(20) = 'cosine',
                    @embedding_type NVARCHAR(128) = NULL,
                    @model_id INT = NULL,
                    @srid INT = 0
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @wkt NVARCHAR(200) = CONCAT('POINT (', @query_spatial_x, ' ', @query_spatial_y, ' ', @query_spatial_z, ')');
                    DECLARE @query_point GEOMETRY = geometry::STGeomFromText(@wkt, @srid);

                    DECLARE @candidates TABLE (
                        AtomEmbeddingId BIGINT PRIMARY KEY,
                        SpatialDistance FLOAT
                    );

                    INSERT INTO @candidates (AtomEmbeddingId, SpatialDistance)
                    SELECT TOP (@spatial_candidates)
                        ae.AtomEmbeddingId,
                        ae.SpatialGeometry.STDistance(@query_point) AS spatial_distance
                    FROM dbo.AtomEmbeddings AS ae
                    WHERE ae.SpatialGeometry IS NOT NULL
                      AND (@embedding_type IS NULL OR ae.EmbeddingType = @embedding_type)
                      AND (@model_id IS NULL OR ae.ModelId = @model_id)
                    ORDER BY ae.SpatialGeometry.STDistance(@query_point);

                    SELECT TOP (@final_top_k)
                        ae.AtomEmbeddingId,
                        ae.AtomId,
                        a.Modality,
                        a.Subtype,
                        a.SourceUri,
                        a.SourceType,
                        a.CanonicalText,
                        ae.EmbeddingType,
                        ae.ModelId,
                        VECTOR_DISTANCE(@distance_metric, ae.EmbeddingVector, @query_vector) AS exact_distance,
                        c.SpatialDistance AS spatial_distance
                    FROM dbo.AtomEmbeddings AS ae
                    INNER JOIN @candidates AS c ON c.AtomEmbeddingId = ae.AtomEmbeddingId
                    INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
                    WHERE ae.EmbeddingVector IS NOT NULL
                    ORDER BY VECTOR_DISTANCE(@distance_metric, ae.EmbeddingVector, @query_vector);
                END;
            ");

            migrationBuilder.Sql(@"
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

                    DECLARE @query_wkt NVARCHAR(200) = CONCAT('POINT Z (', @query_x, ' ', @query_y, ' ', @query_z, ')');
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
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE dbo.sp_CognitiveActivation
                    @query_embedding VECTOR(1998),
                    @activation_threshold FLOAT = 0.8,
                    @max_activated INT = 50
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @query_embedding IS NULL
                    BEGIN
                        RAISERROR('Query embedding cannot be NULL.', 16, 1);
                        RETURN;
                    END;

                    IF @activation_threshold IS NULL OR @activation_threshold <= -1.0 OR @activation_threshold > 1.0
                    BEGIN
                        RAISERROR('Activation threshold must be within (-1, 1].', 16, 1);
                        RETURN;
                    END;

                    DECLARE @start_time DATETIME2 = SYSUTCDATETIME();
                    DECLARE @max_distance FLOAT = 1.0 - @activation_threshold;

                    IF @max_distance <= 0
                    BEGIN
                        RAISERROR('Activation threshold too high for cosine similarity search.', 16, 1);
                        RETURN;
                    END;

                    DECLARE @activated TABLE (
                        AtomEmbeddingId BIGINT PRIMARY KEY,
                        AtomId BIGINT NOT NULL,
                        ActivationStrength FLOAT NOT NULL
                    );

                    INSERT INTO @activated (AtomEmbeddingId, AtomId, ActivationStrength)
                    SELECT TOP (@max_activated)
                        ae.AtomEmbeddingId,
                        ae.AtomId,
                        1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_embedding) AS ActivationStrength
                    FROM dbo.AtomEmbeddings AS ae
                    WHERE ae.EmbeddingVector IS NOT NULL
                      AND VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_embedding) <= @max_distance
                    ORDER BY VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_embedding);

                    DECLARE @activated_count INT = @@ROWCOUNT;
                    DECLARE @duration_ms INT = DATEDIFF(MILLISECOND, @start_time, SYSUTCDATETIME());

                    SELECT
                        ae.AtomEmbeddingId,
                        ae.AtomId,
                        a.Modality,
                        a.Subtype,
                        a.SourceType,
                        a.SourceUri,
                        a.CanonicalText,
                        act.ActivationStrength,
                        CASE
                            WHEN act.ActivationStrength >= 0.95 THEN 'VERY_HIGH'
                            WHEN act.ActivationStrength >= 0.90 THEN 'HIGH'
                            WHEN act.ActivationStrength >= 0.85 THEN 'MEDIUM'
                            ELSE 'LOW'
                        END AS ActivationLevel
                    FROM @activated AS act
                    INNER JOIN dbo.AtomEmbeddings AS ae ON ae.AtomEmbeddingId = act.AtomEmbeddingId
                    INNER JOIN dbo.Atoms AS a ON a.AtomId = act.AtomId
                    ORDER BY act.ActivationStrength DESC;

                    DECLARE @input_json NVARCHAR(MAX) = JSON_OBJECT('activationThreshold': @activation_threshold, 'maxActivated': @max_activated);
                    DECLARE @output_json NVARCHAR(MAX) = JSON_OBJECT('activatedCount': @activated_count);
                    DECLARE @output_metadata NVARCHAR(MAX) = JSON_OBJECT('status': 'completed', 'durationMs': @duration_ms);

                    INSERT INTO dbo.InferenceRequests (TaskType, InputData, ModelsUsed, EnsembleStrategy, OutputData, OutputMetadata, TotalDurationMs)
                    VALUES (
                        'cognitive_activation',
                        CAST(@input_json AS JSON),
                        CAST('""atom_embeddings""' AS JSON),
                        'cognitive_activation',
                        CAST(@output_json AS JSON),
                        CAST(@output_metadata AS JSON),
                        @duration_ms
                    );
                END;
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE dbo.sp_SpatialAttention
                    @query_token_id INT,
                    @context_size INT = 5
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @query_spatial GEOMETRY;
                    SELECT @query_spatial = SpatialProjection
                    FROM dbo.TokenEmbeddingsGeo
                    WHERE TokenId = @query_token_id;

                    IF @query_spatial IS NULL
                    BEGIN
                        RETURN;
                    END;

                                        SELECT TOP (@context_size)
                                                te.TokenId,
                                                te.TokenText,
                                                te.SpatialProjection.STDistance(@query_spatial) AS spatial_distance,
                                                1.0 / (1.0 + te.SpatialProjection.STDistance(@query_spatial)) AS attention_weight,
                                                CASE
                                                        WHEN te.CoarseSpatial.STDistance(@query_spatial) < 2.0 THEN 'COARSE_MATCH'
                                                        WHEN te.FineSpatial.STDistance(@query_spatial) < 0.5 THEN 'FINE_MATCH'
                            ELSE 'MID_MATCH'
                        END AS resolution_level
                    FROM dbo.TokenEmbeddingsGeo AS te WITH (INDEX(idx_spatial_embedding))
                                        WHERE te.TokenId <> @query_token_id
                                            AND te.SpatialProjection.STDistance(@query_spatial) IS NOT NULL
                                        ORDER BY te.SpatialProjection.STDistance(@query_spatial);
                END;
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE dbo.sp_SpatialNextToken
                    @context_tokens NVARCHAR(MAX),
                    @temperature FLOAT = 1.0
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @context_centroid GEOMETRY;

                    SELECT @context_centroid = geometry::STGeomFromText(
                        'POINT(' +
                            CAST(AVG(SpatialProjection.STX) AS NVARCHAR(50)) + ' ' +
                            CAST(AVG(SpatialProjection.STY) AS NVARCHAR(50)) + ' ' +
                            CAST(AVG(CAST(COALESCE(SpatialProjection.Z, 0) AS FLOAT)) AS NVARCHAR(50)) + ')',
                        0)
                    FROM dbo.TokenEmbeddingsGeo
                    WHERE TokenId IN (SELECT value FROM STRING_SPLIT(@context_tokens, ','));

                    IF @context_centroid IS NULL
                    BEGIN
                        RETURN;
                    END;

                    SELECT TOP 3
                        TokenId,
                        TokenText,
                        SpatialProjection.STDistance(@context_centroid) AS distance,
                        EXP(-1 * SpatialProjection.STDistance(@context_centroid) / NULLIF(@temperature, 0.0)) AS probability_score
                    FROM dbo.TokenEmbeddingsGeo WITH (INDEX(idx_spatial_embedding))
                    WHERE SpatialProjection.STDistance(@context_centroid) IS NOT NULL
                    ORDER BY SpatialProjection.STDistance(@context_centroid);
                END;
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE dbo.sp_GenerateTextSpatial
                    @prompt NVARCHAR(MAX),
                    @max_tokens INT = 10
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @context TABLE (token_id INT, token_text NVARCHAR(100));

                    INSERT INTO @context (token_text)
                    SELECT LTRIM(RTRIM(value))
                    FROM STRING_SPLIT(@prompt, ' ');

                    UPDATE c
                    SET c.token_id = te.TokenId
                    FROM @context c
                    INNER JOIN dbo.TokenEmbeddingsGeo te ON LOWER(c.token_text) = LOWER(te.TokenText);

                    DECLARE @generated_text NVARCHAR(MAX) = @prompt;
                    DECLARE @iteration INT = 0;
                    DECLARE @context_ids NVARCHAR(MAX);

                    WHILE @iteration < @max_tokens
                    BEGIN
                        SELECT @context_ids = STRING_AGG(CAST(token_id AS NVARCHAR), ',')
                        FROM @context
                        WHERE token_id IS NOT NULL;

                        IF @context_ids IS NULL
                        BEGIN
                            BREAK;
                        END;

                        DECLARE @next_token_id INT;
                        DECLARE @next_token_text NVARCHAR(100);

                        SELECT TOP 1
                            @next_token_id = TokenId,
                            @next_token_text = TokenText
                        FROM (
                            SELECT TOP 3
                                TokenId,
                                TokenText,
                                SpatialProjection.STDistance(
                                    (SELECT geometry::STGeomFromText(
                                        'POINT(' +
                                        CAST(AVG(SpatialProjection.STX) AS NVARCHAR(50)) + ' ' +
                                        CAST(AVG(SpatialProjection.STY) AS NVARCHAR(50)) + ' ' +
                                        CAST(AVG(CAST(COALESCE(SpatialProjection.Z, 0) AS FLOAT)) AS NVARCHAR(50)) + ')',
                                        0)
                                     FROM dbo.TokenEmbeddingsGeo
                                     WHERE TokenId IN (SELECT value FROM STRING_SPLIT(@context_ids, ',')))
                                ) AS distance
                            FROM dbo.TokenEmbeddingsGeo WITH (INDEX(idx_spatial_embedding))
                            WHERE TokenId NOT IN (SELECT value FROM STRING_SPLIT(@context_ids, ','))
                            ORDER BY distance ASC
                        ) ranked
                        ORDER BY NEWID();

                        IF @next_token_id IS NULL
                        BEGIN
                            BREAK;
                        END;

                        INSERT INTO @context (token_id, token_text)
                        VALUES (@next_token_id, @next_token_text);

                        SET @generated_text = CONCAT(@generated_text, ' ', @next_token_text);
                        SET @iteration += 1;
                    END;

                    SELECT
                        @prompt AS original_prompt,
                        @generated_text AS generated_text,
                        @max_tokens AS tokens_generated,
                        'SPATIAL_GEOMETRY' AS method;
                END;
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE dbo.sp_InitializeSpatialAnchors
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DELETE FROM dbo.SpatialAnchors;

                    DECLARE @available INT = (
                        SELECT COUNT(*)
                        FROM dbo.AtomEmbeddings
                        WHERE EmbeddingVector IS NOT NULL
                    );

                    IF @available < 3
                    BEGIN
                        RAISERROR('Spatial anchor initialization requires at least three AtomEmbeddings rows with stored vectors.', 16, 1);
                        RETURN;
                    END;

                    DECLARE @anchor1Id BIGINT, @anchor2Id BIGINT, @anchor3Id BIGINT;
                    DECLARE @anchor1 VECTOR(1998), @anchor2 VECTOR(1998), @anchor3 VECTOR(1998);

                    SELECT TOP (1)
                        @anchor1Id = AtomEmbeddingId,
                        @anchor1 = EmbeddingVector
                    FROM dbo.AtomEmbeddings
                    WHERE EmbeddingVector IS NOT NULL
                    ORDER BY NEWID();

                    INSERT INTO dbo.SpatialAnchors (AnchorId, AnchorName, AnchorVector, SelectionMethod)
                    VALUES (1, 'Primary Anchor', @anchor1, 'random');

                    SELECT TOP (1)
                        @anchor2Id = AtomEmbeddingId,
                        @anchor2 = EmbeddingVector
                    FROM dbo.AtomEmbeddings
                    WHERE EmbeddingVector IS NOT NULL
                      AND AtomEmbeddingId <> @anchor1Id
                    ORDER BY VECTOR_DISTANCE('euclidean', EmbeddingVector, @anchor1) DESC;

                    IF @anchor2Id IS NULL
                    BEGIN
                        RAISERROR('Unable to select a second spatial anchor.', 16, 1);
                        RETURN;
                    END;

                    INSERT INTO dbo.SpatialAnchors (AnchorId, AnchorName, AnchorVector, SelectionMethod)
                    VALUES (2, 'Distant Anchor', @anchor2, 'maximal_distance');

                    SELECT TOP (1)
                        @anchor3Id = AtomEmbeddingId,
                        @anchor3 = EmbeddingVector
                    FROM dbo.AtomEmbeddings
                    WHERE EmbeddingVector IS NOT NULL
                      AND AtomEmbeddingId NOT IN (@anchor1Id, @anchor2Id)
                    ORDER BY VECTOR_DISTANCE('euclidean', EmbeddingVector, @anchor1) +
                             VECTOR_DISTANCE('euclidean', EmbeddingVector, @anchor2) DESC;

                    IF @anchor3Id IS NULL
                    BEGIN
                        RAISERROR('Unable to select a third spatial anchor.', 16, 1);
                        RETURN;
                    END;

                    INSERT INTO dbo.SpatialAnchors (AnchorId, AnchorName, AnchorVector, SelectionMethod)
                    VALUES (3, 'Triangulation Anchor', @anchor3, 'maximal_combined_distance');
                END;
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE dbo.sp_ComputeSpatialProjection
                    @input_vector VECTOR(1998),
                    @input_dimension INT,
                    @output_x FLOAT OUTPUT,
                    @output_y FLOAT OUTPUT,
                    @output_z FLOAT OUTPUT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF (@input_dimension <= 0 OR @input_dimension > 1998)
                    BEGIN
                        RAISERROR('Input dimension %d is outside the supported range (1-1998).', 16, 1, @input_dimension);
                        RETURN;
                    END;

                    DECLARE @anchor1 VECTOR(1998), @anchor2 VECTOR(1998), @anchor3 VECTOR(1998);

                    SELECT @anchor1 = AnchorVector FROM dbo.SpatialAnchors WHERE AnchorId = 1;
                    SELECT @anchor2 = AnchorVector FROM dbo.SpatialAnchors WHERE AnchorId = 2;
                    SELECT @anchor3 = AnchorVector FROM dbo.SpatialAnchors WHERE AnchorId = 3;

                    IF @anchor1 IS NULL OR @anchor2 IS NULL OR @anchor3 IS NULL
                    BEGIN
                        EXEC dbo.sp_InitializeSpatialAnchors;
                        SELECT @anchor1 = AnchorVector FROM dbo.SpatialAnchors WHERE AnchorId = 1;
                        SELECT @anchor2 = AnchorVector FROM dbo.SpatialAnchors WHERE AnchorId = 2;
                        SELECT @anchor3 = AnchorVector FROM dbo.SpatialAnchors WHERE AnchorId = 3;

                        IF @anchor1 IS NULL OR @anchor2 IS NULL OR @anchor3 IS NULL
                        BEGIN
                            RAISERROR('Spatial anchors are not available.', 16, 1);
                            RETURN;
                        END;
                    END;

                    DECLARE @distance1 FLOAT = VECTOR_DISTANCE('euclidean', @input_vector, @anchor1);
                    DECLARE @distance2 FLOAT = VECTOR_DISTANCE('euclidean', @input_vector, @anchor2);
                    DECLARE @distance3 FLOAT = VECTOR_DISTANCE('euclidean', @input_vector, @anchor3);

                    DECLARE @a12 FLOAT = VECTOR_DISTANCE('euclidean', @anchor1, @anchor2);
                    DECLARE @a13 FLOAT = VECTOR_DISTANCE('euclidean', @anchor1, @anchor3);
                    DECLARE @a23 FLOAT = VECTOR_DISTANCE('euclidean', @anchor2, @anchor3);

                    DECLARE @scale FLOAT = (
                        SELECT MAX(v)
                        FROM (VALUES (@a12), (@a13), (@a23), (1.0)) AS distances(v)
                    );

                    IF @scale IS NULL OR @scale = 0
                    BEGIN
                        SET @scale = 1.0;
                    END;

                    SET @output_x = @distance1 / @scale;
                    SET @output_y = @distance2 / @scale;
                    SET @output_z = @distance3 / @scale;

                    SELECT @output_x AS x, @output_y AS y, @output_z AS z;
                END;
            ");

            migrationBuilder.Sql(@"
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
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE dbo.sp_EnsembleInference
                    @inputData NVARCHAR(MAX),
                    @modelIds NVARCHAR(MAX),
                    @taskType NVARCHAR(50)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @input_json NVARCHAR(MAX) = CASE
                        WHEN @inputData IS NULL OR LTRIM(RTRIM(@inputData)) = '' THEN JSON_OBJECT('payload': '')
                        WHEN ISJSON(@inputData) = 1 THEN @inputData
                        ELSE JSON_OBJECT('payload': @inputData)
                    END;

                    DECLARE @models_json NVARCHAR(MAX) = CASE
                        WHEN @modelIds IS NULL OR LTRIM(RTRIM(@modelIds)) = '' THEN '[]'
                        ELSE '[''' + REPLACE(@modelIds, ',', ''',''') + ''']'
                    END;

                    DECLARE @inference_id BIGINT;

                    INSERT INTO dbo.InferenceRequests
                    (
                        TaskType,
                        InputData,
                        ModelsUsed,
                        EnsembleStrategy,
                        OutputData,
                        OutputMetadata,
                        TotalDurationMs
                    )
                    VALUES
                    (
                        COALESCE(@taskType, 'ensemble_search'),
                        CAST(@input_json AS JSON),
                        CAST(@models_json AS JSON),
                        'weighted_average',
                        CAST(JSON_OBJECT('status': 'queued') AS JSON),
                        CAST(JSON_OBJECT('models': @models_json) AS JSON),
                        0
                    );

                    SET @inference_id = SCOPE_IDENTITY();

                    SELECT JSON_OBJECT('inferenceId': @inference_id, 'models': @modelIds) AS result;
                END;
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE dbo.sp_QueryModelWeights
                    @model_id INT,
                    @layer_name NVARCHAR(200) = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @layer_name IS NULL
                    BEGIN
                        SELECT
                            LayerId,
                            LayerIdx,
                            LayerName,
                            LayerType,
                            ParameterCount,
                            QuantizationType,
                            CacheHitRate
                        FROM dbo.ModelLayers
                        WHERE ModelId = @model_id
                        ORDER BY LayerIdx;
                    END
                    ELSE
                    BEGIN
                        SELECT
                            LayerId,
                            LayerName,
                            LayerType,
                            WeightsGeometry,
                            Parameters,
                            ParameterCount,
                            QuantizationType,
                            QuantizationScale,
                            QuantizationZeroPoint
                        FROM dbo.ModelLayers
                        WHERE ModelId = @model_id AND LayerName = @layer_name;
                    END;
                END;
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE dbo.sp_UpdateModelWeightsFromFeedback
                    @model_id INT,
                    @min_rating TINYINT = 4,
                    @update_magnitude FLOAT = 0.01
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @good_inferences TABLE (
                        InferenceId BIGINT,
                        UserRating TINYINT,
                        LayerId BIGINT
                    );

                    INSERT INTO @good_inferences
                    SELECT
                        ir.InferenceId,
                        ir.UserRating,
                        ist.LayerId
                    FROM dbo.InferenceRequests ir
                    JOIN dbo.InferenceSteps ist ON ir.InferenceId = ist.InferenceId
                    WHERE ir.ModelId = @model_id
                      AND ir.UserRating IS NOT NULL
                      AND ir.UserRating >= @min_rating;

                    IF EXISTS (SELECT 1 FROM @good_inferences)
                    BEGIN
                        SELECT
                            ml.LayerName,
                            COUNT(*) AS FeedbackCount,
                            AVG(CAST(gi.UserRating AS FLOAT)) AS AvgRating
                        FROM @good_inferences gi
                        JOIN dbo.ModelLayers ml ON gi.LayerId = ml.LayerId
                        GROUP BY ml.LayerName
                        ORDER BY FeedbackCount DESC;
                    END
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_UpdateModelWeightsFromFeedback;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_QueryModelWeights;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_EnsembleInference;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_RecomputeAllSpatialProjections;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_ComputeSpatialProjection;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_InitializeSpatialAnchors;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_GenerateTextSpatial;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_SpatialNextToken;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_SpatialAttention;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_CognitiveActivation;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_MultiResolutionSearch;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_ApproxSpatialSearch;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_HybridSearch;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_ExactVectorSearch;");

            migrationBuilder.DropTable(
                name: "AtomEmbeddingComponents",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AtomicAudioSamples",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AtomicPixels",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AtomicTextTokens",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TokenEmbeddingsGeo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "SpatialAnchors",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AtomRelations",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AudioFrames",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "CachedActivations",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "DeduplicationPolicies",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Embeddings_Production",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ImagePatches",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "InferenceSteps",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "IngestionJobAtoms",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ModelMetadata",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TensorAtomCoefficients",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TextDocuments",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TokenVocabulary",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "VideoFrames",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AtomEmbeddings",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AudioData",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Images",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "InferenceRequests",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "IngestionJobs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TensorAtoms",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Videos",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Atoms",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ModelLayers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Models",
                schema: "dbo");
        }
    }
}

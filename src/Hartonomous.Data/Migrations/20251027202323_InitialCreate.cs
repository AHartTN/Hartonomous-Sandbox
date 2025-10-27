using System;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hartonomous.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

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
                    Dimension = table.Column<int>(type: "int", nullable: false, defaultValue: 768),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    AccessCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastAccessed = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Embeddings_Production", x => x.EmbeddingId);
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
                    Weights = table.Column<SqlVector<float>>(type: "VECTOR(1998)", nullable: true),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CachedActivations",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Embeddings_Production",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "InferenceSteps",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ModelMetadata",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TokenVocabulary",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ModelLayers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "InferenceRequests",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Models",
                schema: "dbo");
        }
    }
}

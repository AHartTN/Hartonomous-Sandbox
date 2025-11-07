using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hartonomous.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "graph");

            migrationBuilder.CreateTable(
                name: "AtomGraphEdges",
                schema: "graph",
                columns: table => new
                {
                    EdgeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EdgeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false, defaultValue: 1.0),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtomGraphEdges", x => x.EdgeId);
                    table.CheckConstraint("CK_AtomGraphEdges_EdgeType", "[EdgeType] IN ('DerivedFrom', 'ComponentOf', 'SimilarTo', 'Uses', 'InputTo', 'OutputFrom', 'BindsToConcept')");
                    table.CheckConstraint("CK_AtomGraphEdges_Weight", "[Weight] >= 0.0 AND [Weight] <= 1.0");
                })
                .Annotation("SqlServer:IsEdge", true);

            migrationBuilder.CreateTable(
                name: "AtomGraphNodes",
                schema: "graph",
                columns: table => new
                {
                    NodeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AtomId = table.Column<long>(type: "bigint", nullable: false),
                    NodeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NodeLabel = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtomGraphNodes", x => x.NodeId);
                    table.ForeignKey(
                        name: "FK_AtomGraphNodes_Atoms_AtomId",
                        column: x => x.AtomId,
                        principalTable: "Atoms",
                        principalColumn: "AtomId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("SqlServer:IsNode", true);

            migrationBuilder.CreateTable(
                name: "AtomPayloadStore",
                columns: table => new
                {
                    PayloadId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RowGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    AtomId = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContentHash = table.Column<byte[]>(type: "binary(32)", maxLength: 32, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    PayloadData = table.Column<byte[]>(type: "VARBINARY(MAX) FILESTREAM", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtomPayloadStore", x => x.PayloadId);
                    table.CheckConstraint("CK_AtomPayloadStore_ContentType", "[ContentType] LIKE '%/%'");
                    table.CheckConstraint("CK_AtomPayloadStore_SizeBytes", "[SizeBytes] > 0");
                    table.ForeignKey(
                        name: "FK_AtomPayloadStore_Atoms_AtomId",
                        column: x => x.AtomId,
                        principalTable: "Atoms",
                        principalColumn: "AtomId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AutonomousImprovementHistory",
                columns: table => new
                {
                    ImprovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    AnalysisResults = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetFile = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EstimatedImpact = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    GitCommitHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SuccessScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    TestsPassed = table.Column<int>(type: "int", nullable: true),
                    TestsFailed = table.Column<int>(type: "int", nullable: true),
                    PerformanceDelta = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WasDeployed = table.Column<bool>(type: "bit", nullable: false),
                    WasRolledBack = table.Column<bool>(type: "bit", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RolledBackAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutonomousImprovementHistory", x => x.ImprovementId);
                    table.CheckConstraint("CK_AutonomousImprovement_SuccessScore", "[SuccessScore] >= 0 AND [SuccessScore] <= 1");
                });

            migrationBuilder.CreateTable(
                name: "BillingUsageLedger",
                columns: table => new
                {
                    LedgerId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrincipalId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Handler = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Units = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    BaseRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Multiplier = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, defaultValue: 1.0m),
                    TotalCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingUsageLedger", x => x.LedgerId);
                });

            migrationBuilder.CreateTable(
                name: "Concepts",
                schema: "provenance",
                columns: table => new
                {
                    ConceptId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConceptName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CentroidVector = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    VectorDimension = table.Column<int>(type: "int", nullable: false),
                    MemberCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CoherenceScore = table.Column<double>(type: "float", nullable: true),
                    SeparationScore = table.Column<double>(type: "float", nullable: true),
                    DiscoveryMethod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModelId = table.Column<int>(type: "int", nullable: false),
                    DiscoveredAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Concepts", x => x.ConceptId);
                    table.ForeignKey(
                        name: "FK_Concepts_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "ModelId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InferenceCache",
                columns: table => new
                {
                    CacheId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CacheKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ModelId = table.Column<int>(type: "int", nullable: false),
                    InferenceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InputHash = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    OutputData = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    IntermediateStates = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    LastAccessedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AccessCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    ComputeTimeMs = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InferenceCache", x => x.CacheId);
                    table.ForeignKey(
                        name: "FK_InferenceCache_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "ModelId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantSecurityPolicy",
                columns: table => new
                {
                    PolicyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PolicyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PolicyType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PolicyRules = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSecurityPolicy", x => x.PolicyId);
                });

            migrationBuilder.CreateTable(
                name: "TestResults",
                columns: table => new
                {
                    TestResultId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TestSuite = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TestStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExecutionTimeMs = table.Column<double>(type: "float", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TestOutput = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Environment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TestCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MemoryUsageMB = table.Column<double>(type: "float", nullable: true),
                    CpuUsagePercent = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestResults", x => x.TestResultId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AtomGraphEdges_CreatedUtc",
                schema: "graph",
                table: "AtomGraphEdges",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AtomGraphEdges_EdgeType",
                schema: "graph",
                table: "AtomGraphEdges",
                column: "EdgeType");

            migrationBuilder.CreateIndex(
                name: "IX_AtomGraphEdges_Weight",
                schema: "graph",
                table: "AtomGraphEdges",
                column: "Weight");

            migrationBuilder.CreateIndex(
                name: "IX_AtomGraphNodes_AtomId",
                schema: "graph",
                table: "AtomGraphNodes",
                column: "AtomId");

            migrationBuilder.CreateIndex(
                name: "IX_AtomGraphNodes_CreatedUtc",
                schema: "graph",
                table: "AtomGraphNodes",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AtomGraphNodes_NodeType",
                schema: "graph",
                table: "AtomGraphNodes",
                column: "NodeType");

            migrationBuilder.CreateIndex(
                name: "IX_AtomPayloadStore_AtomId",
                table: "AtomPayloadStore",
                column: "AtomId");

            migrationBuilder.CreateIndex(
                name: "IX_AtomPayloadStore_RowGuid",
                table: "AtomPayloadStore",
                column: "RowGuid");

            migrationBuilder.CreateIndex(
                name: "UX_AtomPayloadStore_ContentHash",
                table: "AtomPayloadStore",
                column: "ContentHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutonomousImprovement_ChangeType_RiskLevel",
                table: "AutonomousImprovementHistory",
                columns: new[] { "ChangeType", "RiskLevel" })
                .Annotation("SqlServer:Include", new[] { "ErrorMessage", "SuccessScore" });

            migrationBuilder.CreateIndex(
                name: "IX_AutonomousImprovement_StartedAt",
                table: "AutonomousImprovementHistory",
                column: "StartedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_AutonomousImprovement_SuccessScore",
                table: "AutonomousImprovementHistory",
                column: "SuccessScore",
                descending: new bool[0],
                filter: "[WasDeployed] = 1 AND [WasRolledBack] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BillingUsageLedger_Operation_Timestamp",
                table: "BillingUsageLedger",
                columns: new[] { "Operation", "TimestampUtc" })
                .Annotation("SqlServer:Include", new[] { "TenantId", "Units", "TotalCost" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingUsageLedger_TenantId_Timestamp",
                table: "BillingUsageLedger",
                columns: new[] { "TenantId", "TimestampUtc" })
                .Annotation("SqlServer:Include", new[] { "Operation", "TotalCost" });

            migrationBuilder.CreateIndex(
                name: "IX_Concepts_CoherenceScore",
                schema: "provenance",
                table: "Concepts",
                column: "CoherenceScore",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Concepts_ConceptName",
                schema: "provenance",
                table: "Concepts",
                column: "ConceptName");

            migrationBuilder.CreateIndex(
                name: "IX_Concepts_DiscoveryMethod",
                schema: "provenance",
                table: "Concepts",
                column: "DiscoveryMethod");

            migrationBuilder.CreateIndex(
                name: "IX_Concepts_ModelId_IsActive",
                schema: "provenance",
                table: "Concepts",
                columns: new[] { "ModelId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_InferenceCache_CacheKey",
                table: "InferenceCache",
                column: "CacheKey");

            migrationBuilder.CreateIndex(
                name: "IX_InferenceCache_LastAccessedUtc",
                table: "InferenceCache",
                column: "LastAccessedUtc",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_InferenceCache_ModelId_InferenceType",
                table: "InferenceCache",
                columns: new[] { "ModelId", "InferenceType" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantSecurityPolicy_EffectiveDates",
                table: "TenantSecurityPolicy",
                columns: new[] { "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantSecurityPolicy_IsActive",
                table: "TenantSecurityPolicy",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TenantSecurityPolicy_TenantId_PolicyType",
                table: "TenantSecurityPolicy",
                columns: new[] { "TenantId", "PolicyType" });

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_ExecutionTimeMs",
                table: "TestResults",
                column: "ExecutionTimeMs",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_TestCategory_ExecutedAt",
                table: "TestResults",
                columns: new[] { "TestCategory", "ExecutedAt" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_TestStatus",
                table: "TestResults",
                column: "TestStatus");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_TestSuite_ExecutedAt",
                table: "TestResults",
                columns: new[] { "TestSuite", "ExecutedAt" },
                descending: new bool[0]);

            // Add SQL Graph connection constraint
            migrationBuilder.Sql(@"
                ALTER TABLE graph.AtomGraphEdges
                ADD CONSTRAINT EC_AtomGraphEdges 
                CONNECTION (graph.AtomGraphNodes TO graph.AtomGraphNodes);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop SQL Graph connection constraint first
            migrationBuilder.Sql(@"
                ALTER TABLE graph.AtomGraphEdges
                DROP CONSTRAINT IF EXISTS EC_AtomGraphEdges;
            ");

            migrationBuilder.DropTable(
                name: "AtomGraphEdges",
                schema: "graph");

            migrationBuilder.DropTable(
                name: "AtomGraphNodes",
                schema: "graph");

            migrationBuilder.DropTable(
                name: "AtomPayloadStore");

            migrationBuilder.DropTable(
                name: "AutonomousImprovementHistory");

            migrationBuilder.DropTable(
                name: "BillingUsageLedger");

            migrationBuilder.DropTable(
                name: "Concepts",
                schema: "provenance");

            migrationBuilder.DropTable(
                name: "InferenceCache");

            migrationBuilder.DropTable(
                name: "TenantSecurityPolicy");

            migrationBuilder.DropTable(
                name: "TestResults");
        }
    }
}

using System;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Hartonomous.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAtomSubstrateTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "UX_DeduplicationPolicies_PolicyName",
                schema: "dbo",
                table: "DeduplicationPolicies",
                column: "PolicyName",
                unique: true);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AtomEmbeddingComponents",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AtomRelations",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "DeduplicationPolicies",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "IngestionJobAtoms",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TensorAtomCoefficients",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AtomEmbeddings",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "IngestionJobs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TensorAtoms",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Atoms",
                schema: "dbo");

        }
    }
}

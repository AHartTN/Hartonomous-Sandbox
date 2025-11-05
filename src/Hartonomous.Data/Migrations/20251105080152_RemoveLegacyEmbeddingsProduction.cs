using System;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Hartonomous.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyEmbeddingsProduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Embeddings_Production");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "LayerTensorSegments",
                type: "DATETIME2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "DATETIME2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "LayerTensorSegments",
                type: "DATETIME2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "DATETIME2",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.CreateTable(
                name: "Embeddings_Production",
                columns: table => new
                {
                    EmbeddingId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Dimension = table.Column<int>(type: "int", nullable: false, defaultValue: 768),
                    EmbeddingFull = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true),
                    EmbeddingModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastAccessed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SourceText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SpatialCoarse = table.Column<Point>(type: "geometry", nullable: true),
                    SpatialGeometry = table.Column<Point>(type: "geometry", nullable: true),
                    SpatialProjX = table.Column<double>(type: "float", nullable: true),
                    SpatialProjY = table.Column<double>(type: "float", nullable: true),
                    SpatialProjZ = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Embeddings_Production", x => x.EmbeddingId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Embeddings_Production_ContentHash",
                table: "Embeddings_Production",
                column: "ContentHash");
        }
    }
}

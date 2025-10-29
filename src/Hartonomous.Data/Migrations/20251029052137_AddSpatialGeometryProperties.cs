using System;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Hartonomous.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSpatialGeometryProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the computed spatial_geometry column from previous migration before adding as regular column
            migrationBuilder.Sql("DROP INDEX IF EXISTS [idx_spatial_fine] ON [dbo].[Embeddings_Production];");
            migrationBuilder.Sql("ALTER TABLE [dbo].[Embeddings_Production] DROP COLUMN IF EXISTS [spatial_geometry];");

            migrationBuilder.AddColumn<Point>(
                name: "spatial_coarse",
                schema: "dbo",
                table: "Embeddings_Production",
                type: "geometry",
                nullable: true);

            migrationBuilder.AddColumn<Point>(
                name: "spatial_geometry",
                schema: "dbo",
                table: "Embeddings_Production",
                type: "geometry",
                nullable: true);

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

            migrationBuilder.Sql(@"
                CREATE SPATIAL INDEX [idx_spatial_coarse] 
                ON [dbo].[Embeddings_Production] ([spatial_coarse])
                USING GEOMETRY_GRID 
                WITH (
                    BOUNDING_BOX = (-1000, -1000, 1000, 1000),
                    GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM),
                    CELLS_PER_OBJECT = 16
                );
            ");

            migrationBuilder.Sql(@"
                CREATE SPATIAL INDEX [idx_spatial_fine] 
                ON [dbo].[Embeddings_Production] ([spatial_geometry])
                USING GEOMETRY_GRID 
                WITH (
                    BOUNDING_BOX = (-1000, -1000, 1000, 1000),
                    GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM),
                    CELLS_PER_OBJECT = 16
                );
            ");

            migrationBuilder.CreateIndex(
                name: "idx_amplitude",
                schema: "dbo",
                table: "AtomicAudioSamples",
                column: "AmplitudeNormalized");

            migrationBuilder.Sql(@"
                CREATE SPATIAL INDEX [idx_color_space] 
                ON [dbo].[AtomicPixels] ([ColorPoint])
                USING GEOMETRY_GRID 
                WITH (
                    BOUNDING_BOX = (0, 0, 255, 255),
                    GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM),
                    CELLS_PER_OBJECT = 16
                );
            ");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AtomicAudioSamples",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AtomicPixels",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AtomicTextTokens",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "idx_spatial_coarse",
                schema: "dbo",
                table: "Embeddings_Production");

            migrationBuilder.DropIndex(
                name: "idx_spatial_fine",
                schema: "dbo",
                table: "Embeddings_Production");

            migrationBuilder.DropColumn(
                name: "spatial_coarse",
                schema: "dbo",
                table: "Embeddings_Production");

            migrationBuilder.DropColumn(
                name: "spatial_geometry",
                schema: "dbo",
                table: "Embeddings_Production");
        }
    }
}

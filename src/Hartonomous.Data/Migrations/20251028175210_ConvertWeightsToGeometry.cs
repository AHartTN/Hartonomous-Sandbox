using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hartonomous.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConvertWeightsToGeometry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop index if exists (idempotent)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_layer_chunks' AND object_id = OBJECT_ID('dbo.ModelLayers'))
                    DROP INDEX idx_layer_chunks ON dbo.ModelLayers;
            ");

            // Drop columns if they exist (idempotent)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ModelLayers') AND name = 'ChunkIdx')
                    ALTER TABLE dbo.ModelLayers DROP COLUMN ChunkIdx;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ModelLayers') AND name = 'TotalChunks')
                    ALTER TABLE dbo.ModelLayers DROP COLUMN TotalChunks;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ModelLayers') AND name = 'Weights')
                    ALTER TABLE dbo.ModelLayers DROP COLUMN Weights;
            ");

            // Add new columns if they don't exist (idempotent)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ModelLayers') AND name = 'TensorDtype')
                    ALTER TABLE dbo.ModelLayers ADD TensorDtype NVARCHAR(20) NULL DEFAULT 'float32';
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ModelLayers') AND name = 'TensorShape')
                    ALTER TABLE dbo.ModelLayers ADD TensorShape NVARCHAR(200) NULL;
            ");

            // Add GEOMETRY column for weights (EF Core doesn't support SqlGeometry mapping)
            // Handled via ADO.NET in repository layer, similar to SqlVector pattern
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ModelLayers') AND name = 'WeightsGeometry')
                    ALTER TABLE dbo.ModelLayers ADD WeightsGeometry GEOMETRY NULL;
            ");

            // Create spatial index for O(log n) queries
            // Bounding box tuned for typical weight ranges
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_weights_spatial' AND object_id = OBJECT_ID('dbo.ModelLayers'))
                BEGIN
                    CREATE SPATIAL INDEX idx_weights_spatial ON dbo.ModelLayers(WeightsGeometry)
                    WITH (
                        BOUNDING_BOX = (-10, -10, 10, 10),
                        GRIDS = (MEDIUM, MEDIUM, MEDIUM, MEDIUM),
                        CELLS_PER_OBJECT = 16
                    );
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop spatial index
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_weights_spatial' AND object_id = OBJECT_ID('dbo.ModelLayers'))
                    DROP INDEX idx_weights_spatial ON dbo.ModelLayers;
            ");

            // Drop GEOMETRY column
            migrationBuilder.Sql(@"
                ALTER TABLE dbo.ModelLayers DROP COLUMN WeightsGeometry;
            ");

            migrationBuilder.DropColumn(
                name: "TensorDtype",
                schema: "dbo",
                table: "ModelLayers");

            migrationBuilder.DropColumn(
                name: "TensorShape",
                schema: "dbo",
                table: "ModelLayers");

            migrationBuilder.AddColumn<int>(
                name: "ChunkIdx",
                schema: "dbo",
                table: "ModelLayers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalChunks",
                schema: "dbo",
                table: "ModelLayers",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<SqlVector<float>>(
                name: "Weights",
                schema: "dbo",
                table: "ModelLayers",
                type: "VECTOR(1998)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_layer_chunks",
                schema: "dbo",
                table: "ModelLayers",
                columns: new[] { "ModelId", "LayerName", "ChunkIdx" });
        }
    }
}

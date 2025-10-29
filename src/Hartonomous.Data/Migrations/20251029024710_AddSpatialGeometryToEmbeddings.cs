using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hartonomous.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSpatialGeometryToEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add computed spatial geometry column for hybrid search
            // This creates a GEOMETRY POINT from the 3D spatial projection coordinates
            migrationBuilder.Sql(@"
                ALTER TABLE [dbo].[Embeddings_Production] 
                ADD [spatial_geometry] AS geometry::STGeomFromText(
                    'POINT(' + 
                    CAST([SpatialProjX] AS NVARCHAR(50)) + ' ' + 
                    CAST([SpatialProjY] AS NVARCHAR(50)) + ')', 0
                ) PERSISTED;
            ");

            // Create spatial index for fast approximate search (O(log n) instead of O(n))
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop spatial index first
            migrationBuilder.Sql("DROP INDEX IF EXISTS [idx_spatial_fine] ON [dbo].[Embeddings_Production];");

            // Drop computed column
            migrationBuilder.Sql("ALTER TABLE [dbo].[Embeddings_Production] DROP COLUMN IF EXISTS [spatial_geometry];");
        }
    }
}

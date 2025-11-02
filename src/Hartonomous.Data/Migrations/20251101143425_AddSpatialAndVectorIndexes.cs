using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hartonomous.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSpatialAndVectorIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.Embeddings_Production') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'idx_spatial_fine'
          AND object_id = OBJECT_ID('dbo.Embeddings_Production'))
    BEGIN
        CREATE SPATIAL INDEX idx_spatial_fine
        ON dbo.Embeddings_Production(spatial_geometry)
        WITH (
            BOUNDING_BOX = (-10, -10, 10, 10),
            GRIDS = (
                LEVEL_1 = HIGH,
                LEVEL_2 = HIGH,
                LEVEL_3 = MEDIUM,
                LEVEL_4 = LOW
            ),
            CELLS_PER_OBJECT = 16
        );
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'idx_spatial_coarse'
          AND object_id = OBJECT_ID('dbo.Embeddings_Production'))
    BEGIN
        CREATE SPATIAL INDEX idx_spatial_coarse
        ON dbo.Embeddings_Production(spatial_coarse)
        WITH (
            BOUNDING_BOX = (-10, -10, 10, 10),
            GRIDS = (
                LEVEL_1 = MEDIUM,
                LEVEL_2 = LOW,
                LEVEL_3 = LOW,
                LEVEL_4 = LOW
            ),
            CELLS_PER_OBJECT = 8
        );
    END;
END;
");

            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.Embeddings_DiskANN') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'idx_diskann_vector'
          AND object_id = OBJECT_ID('dbo.Embeddings_DiskANN'))
    BEGIN
        DROP INDEX idx_diskann_vector ON dbo.Embeddings_DiskANN;
    END;

    CREATE VECTOR INDEX idx_diskann_vector
    ON dbo.Embeddings_DiskANN(embedding_full)
    WITH (
        METRIC = 'cosine',
        TYPE = 'DiskANN',
        MAXDOP = 0
    );
END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.Embeddings_Production') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'idx_spatial_fine'
          AND object_id = OBJECT_ID('dbo.Embeddings_Production'))
    BEGIN
        DROP INDEX idx_spatial_fine ON dbo.Embeddings_Production;
    END;

    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'idx_spatial_coarse'
          AND object_id = OBJECT_ID('dbo.Embeddings_Production'))
    BEGIN
        DROP INDEX idx_spatial_coarse ON dbo.Embeddings_Production;
    END;
END;
");

            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.Embeddings_DiskANN') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'idx_diskann_vector'
          AND object_id = OBJECT_ID('dbo.Embeddings_DiskANN'))
    BEGIN
        DROP INDEX idx_diskann_vector ON dbo.Embeddings_DiskANN;
    END;
END;
");
        }
    }
}

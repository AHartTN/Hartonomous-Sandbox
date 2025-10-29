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
            // NOTE: Removed computed spatial_geometry column - now handled as regular columns in later migration
            // The spatial geometry columns are now regular writable columns managed by EF Core
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // NOTE: No longer dropping computed spatial_geometry column - now handled as regular columns in later migration
        }
    }
}

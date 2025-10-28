using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hartonomous.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTensorChunkingSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "idx_layer_chunks",
                schema: "dbo",
                table: "ModelLayers",
                columns: new[] { "ModelId", "LayerName", "ChunkIdx" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_layer_chunks",
                schema: "dbo",
                table: "ModelLayers");

            migrationBuilder.DropColumn(
                name: "ChunkIdx",
                schema: "dbo",
                table: "ModelLayers");

            migrationBuilder.DropColumn(
                name: "TotalChunks",
                schema: "dbo",
                table: "ModelLayers");
        }
    }
}

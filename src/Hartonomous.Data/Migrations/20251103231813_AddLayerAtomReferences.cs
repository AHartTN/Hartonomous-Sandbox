using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hartonomous.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLayerAtomReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LayerAtomId",
                table: "ModelLayers",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModelLayers_LayerAtomId",
                table: "ModelLayers",
                column: "LayerAtomId");

            migrationBuilder.AddForeignKey(
                name: "FK_ModelLayers_Atoms_LayerAtomId",
                table: "ModelLayers",
                column: "LayerAtomId",
                principalTable: "Atoms",
                principalColumn: "AtomId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ModelLayers_Atoms_LayerAtomId",
                table: "ModelLayers");

            migrationBuilder.DropIndex(
                name: "IX_ModelLayers_LayerAtomId",
                table: "ModelLayers");

            migrationBuilder.DropColumn(
                name: "LayerAtomId",
                table: "ModelLayers");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hartonomous.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContentHashAndRepositoryMethods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                schema: "dbo",
                table: "Embeddings_Production",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_content_hash",
                schema: "dbo",
                table: "Embeddings_Production",
                column: "ContentHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_content_hash",
                schema: "dbo",
                table: "Embeddings_Production");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                schema: "dbo",
                table: "Embeddings_Production");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hartonomous.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAutonomousMetadataToInferenceRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Complexity",
                table: "InferenceRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlaTier",
                table: "InferenceRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedResponseTimeMs",
                table: "InferenceRequests",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Complexity",
                table: "InferenceRequests");

            migrationBuilder.DropColumn(
                name: "SlaTier",
                table: "InferenceRequests");

            migrationBuilder.DropColumn(
                name: "EstimatedResponseTimeMs",
                table: "InferenceRequests");
        }
    }
}

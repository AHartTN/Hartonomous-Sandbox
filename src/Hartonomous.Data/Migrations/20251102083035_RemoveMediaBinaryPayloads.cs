using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hartonomous.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMediaBinaryPayloads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RawData",
                schema: "dbo",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "RawData",
                schema: "dbo",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "TextureFeatures",
                schema: "dbo",
                table: "ImagePatches");

            migrationBuilder.DropColumn(
                name: "RawData",
                schema: "dbo",
                table: "AudioData");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RawData",
                schema: "dbo",
                table: "Videos",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RawData",
                schema: "dbo",
                table: "Images",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "TextureFeatures",
                schema: "dbo",
                table: "ImagePatches",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RawData",
                schema: "dbo",
                table: "AudioData",
                type: "varbinary(max)",
                nullable: true);
        }
    }
}

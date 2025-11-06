using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hartonomous.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProvenanceToGenerationStreams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayloadSizeBytes",
                schema: "provenance",
                table: "GenerationStreams");

            migrationBuilder.DropColumn(
                name: "Stream",
                schema: "provenance",
                table: "GenerationStreams");

            migrationBuilder.AddColumn<string>(
                name: "ContextMetadata",
                schema: "provenance",
                table: "GenerationStreams",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeneratedAtomIds",
                schema: "provenance",
                table: "GenerationStreams",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "GenerationStreamId",
                schema: "provenance",
                table: "GenerationStreams",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "ModelId",
                schema: "provenance",
                table: "GenerationStreams",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ProvenanceStream",
                schema: "provenance",
                table: "GenerationStreams",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                schema: "provenance",
                table: "GenerationStreams",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_GenerationStreams_CreatedUtc",
                schema: "provenance",
                table: "GenerationStreams",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationStreams_GenerationStreamId",
                schema: "provenance",
                table: "GenerationStreams",
                column: "GenerationStreamId");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationStreams_ModelId",
                schema: "provenance",
                table: "GenerationStreams",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationStreams_TenantId",
                schema: "provenance",
                table: "GenerationStreams",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_GenerationStreams_Models",
                schema: "provenance",
                table: "GenerationStreams",
                column: "ModelId",
                principalTable: "Models",
                principalColumn: "ModelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GenerationStreams_Models",
                schema: "provenance",
                table: "GenerationStreams");

            migrationBuilder.DropIndex(
                name: "IX_GenerationStreams_CreatedUtc",
                schema: "provenance",
                table: "GenerationStreams");

            migrationBuilder.DropIndex(
                name: "IX_GenerationStreams_GenerationStreamId",
                schema: "provenance",
                table: "GenerationStreams");

            migrationBuilder.DropIndex(
                name: "IX_GenerationStreams_ModelId",
                schema: "provenance",
                table: "GenerationStreams");

            migrationBuilder.DropIndex(
                name: "IX_GenerationStreams_TenantId",
                schema: "provenance",
                table: "GenerationStreams");

            migrationBuilder.DropColumn(
                name: "ContextMetadata",
                schema: "provenance",
                table: "GenerationStreams");

            migrationBuilder.DropColumn(
                name: "GeneratedAtomIds",
                schema: "provenance",
                table: "GenerationStreams");

            migrationBuilder.DropColumn(
                name: "GenerationStreamId",
                schema: "provenance",
                table: "GenerationStreams");

            migrationBuilder.DropColumn(
                name: "ModelId",
                schema: "provenance",
                table: "GenerationStreams");

            migrationBuilder.DropColumn(
                name: "ProvenanceStream",
                schema: "provenance",
                table: "GenerationStreams");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "provenance",
                table: "GenerationStreams");

            migrationBuilder.AddColumn<byte[]>(
                name: "Stream",
                schema: "provenance",
                table: "GenerationStreams",
                type: "provenance.AtomicStream",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<long>(
                name: "PayloadSizeBytes",
                schema: "provenance",
                table: "GenerationStreams",
                type: "bigint",
                nullable: false,
                computedColumnSql: "CONVERT(BIGINT, DATALENGTH([Stream]))",
                stored: true);
        }
    }
}

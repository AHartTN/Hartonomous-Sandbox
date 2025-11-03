using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hartonomous.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGenerationStreams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "provenance");

            migrationBuilder.CreateTable(
                name: "GenerationStreams",
                schema: "provenance",
                columns: table => new
                {
                    StreamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Stream = table.Column<byte[]>(type: "provenance.AtomicStream", nullable: false),
                    PayloadSizeBytes = table.Column<long>(type: "bigint", nullable: false, computedColumnSql: "CONVERT(BIGINT, DATALENGTH([Stream]))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenerationStreams", x => x.StreamId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GenerationStreams_Model",
                schema: "provenance",
                table: "GenerationStreams",
                column: "Model");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationStreams_Scope",
                schema: "provenance",
                table: "GenerationStreams",
                column: "Scope");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GenerationStreams",
                schema: "provenance");
        }
    }
}

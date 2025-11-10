using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hartonomous.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncWithSqlSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TokenVocabulary_ModelId_TokenId') DROP INDEX [IX_TokenVocabulary_ModelId_TokenId] ON [TokenVocabulary];", suppressTransaction: true);

            migrationBuilder.Sql(@"
                IF COL_LENGTH('TokenVocabulary', 'TokenId1') IS NOT NULL
                BEGIN
                    DECLARE @var nvarchar(max);
                    SELECT @var = QUOTENAME([d].[name]) FROM [sys].[default_constraints] [d]
                    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
                    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TokenVocabulary]') AND [c].[name] = N'TokenId1');
                    IF @var IS NOT NULL EXEC(N'ALTER TABLE [TokenVocabulary] DROP CONSTRAINT ' + @var + ';');
                    ALTER TABLE [TokenVocabulary] DROP COLUMN [TokenId1];
                END
            ", suppressTransaction: true);

            migrationBuilder.AlterColumn<long>(
                name: "Frequency",
                table: "TokenVocabulary",
                type: "bigint",
                nullable: false,
                defaultValue: 1L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldDefaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "Frequency",
                table: "TokenVocabulary",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldDefaultValue: 1L);

            migrationBuilder.AddColumn<int>(
                name: "TokenId1",
                table: "TokenVocabulary",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TokenVocabulary_ModelId_TokenId",
                table: "TokenVocabulary",
                columns: new[] { "ModelId", "TokenId1" },
                unique: true);
        }
    }
}

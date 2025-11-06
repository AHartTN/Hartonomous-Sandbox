using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hartonomous.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTemporalTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert core tables to temporal tables with system versioning
            // This enables point-in-time queries (FOR SYSTEM_TIME AS OF @date)
            
            var tables = new[]
            {
                "Atoms",
                "AtomEmbeddings",
                "AtomRelations",
                "Models",
                "ModelLayers",
                "InferenceRequests",
                "InferenceSteps"
            };

            foreach (var table in tables)
            {
                // Add ValidFrom and ValidTo columns
                migrationBuilder.Sql($@"
                    ALTER TABLE dbo.{table} 
                    ADD 
                        ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL DEFAULT SYSUTCDATETIME(),
                        ValidTo DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL DEFAULT CAST('9999-12-31 23:59:59.9999999' AS DATETIME2),
                        PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo);
                ");

                // Determine retention period based on table type
                // All operational tables use 90 day retention for now
                var retentionPeriod = "90 DAYS";

                // Enable system versioning with history table
                migrationBuilder.Sql($@"
                    ALTER TABLE dbo.{table}
                    SET (SYSTEM_VERSIONING = ON (
                        HISTORY_TABLE = dbo.{table}History,
                        HISTORY_RETENTION_PERIOD = {retentionPeriod}
                    ));
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var tables = new[]
            {
                "Atoms",
                "AtomEmbeddings",
                "AtomRelations",
                "Models",
                "ModelLayers",
                "InferenceRequests",
                "InferenceSteps"
            };

            foreach (var table in tables)
            {
                // Disable system versioning
                migrationBuilder.Sql($@"
                    ALTER TABLE dbo.{table} SET (SYSTEM_VERSIONING = OFF);
                ");

                // Drop history table
                migrationBuilder.Sql($@"
                    DROP TABLE IF EXISTS dbo.{table}History;
                ");

                // Remove temporal columns
                migrationBuilder.Sql($@"
                    ALTER TABLE dbo.{table} DROP PERIOD FOR SYSTEM_TIME;
                    ALTER TABLE dbo.{table} DROP COLUMN ValidFrom, ValidTo;
                ");
            }
        }
    }
}

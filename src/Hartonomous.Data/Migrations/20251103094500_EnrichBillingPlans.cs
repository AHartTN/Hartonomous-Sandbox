using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hartonomous.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnrichBillingPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowsPrivateData",
                table: "BillingRatePlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanQueryPublicCorpus",
                table: "BillingRatePlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "IncludedPrivateStorageGb",
                table: "BillingRatePlans",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "IncludedPublicStorageGb",
                table: "BillingRatePlans",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "IncludedSeatCount",
                table: "BillingRatePlans",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyFee",
                table: "BillingRatePlans",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PlanCode",
                table: "BillingRatePlans",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPricePerDcu",
                table: "BillingRatePlans",
                type: "decimal(18,6)",
                nullable: false,
                defaultValue: 0.00008m);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "BillingOperationRates",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "BillingOperationRates",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitOfMeasure",
                table: "BillingOperationRates",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "UX_BillingRatePlans_Tenant_PlanCode",
                table: "BillingRatePlans",
                columns: new[] { "TenantId", "PlanCode" },
                unique: true,
                filter: "[PlanCode] <> ''");

            migrationBuilder.Sql(@"
UPDATE rp
SET PlanCode = LOWER(REPLACE(COALESCE(NULLIF(rp.Name, ''), 'plan'), ' ', '_')) + '_' + LEFT(CONVERT(varchar(36), rp.RatePlanId), 8)
FROM dbo.BillingRatePlans rp
WHERE ISNULL(rp.PlanCode, '') = '';
");

            migrationBuilder.Sql(@"
UPDATE rate
SET UnitOfMeasure = 'dcu'
FROM dbo.BillingOperationRates rate
WHERE ISNULL(rate.UnitOfMeasure, '') = '';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_BillingRatePlans_Tenant_PlanCode",
                table: "BillingRatePlans");

            migrationBuilder.DropColumn(
                name: "AllowsPrivateData",
                table: "BillingRatePlans");

            migrationBuilder.DropColumn(
                name: "CanQueryPublicCorpus",
                table: "BillingRatePlans");

            migrationBuilder.DropColumn(
                name: "IncludedPrivateStorageGb",
                table: "BillingRatePlans");

            migrationBuilder.DropColumn(
                name: "IncludedPublicStorageGb",
                table: "BillingRatePlans");

            migrationBuilder.DropColumn(
                name: "IncludedSeatCount",
                table: "BillingRatePlans");

            migrationBuilder.DropColumn(
                name: "MonthlyFee",
                table: "BillingRatePlans");

            migrationBuilder.DropColumn(
                name: "PlanCode",
                table: "BillingRatePlans");

            migrationBuilder.DropColumn(
                name: "UnitPricePerDcu",
                table: "BillingRatePlans");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "BillingOperationRates");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "BillingOperationRates");

            migrationBuilder.DropColumn(
                name: "UnitOfMeasure",
                table: "BillingOperationRates");
        }
    }
}

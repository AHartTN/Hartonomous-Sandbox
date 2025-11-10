global using System;
global using System.IO;
global using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("=== EF Core Schema Extractor ===\n");

// Build EF Core services
var services = new ServiceCollection();
services.AddDbContext<Hartonomous.Data.HartonomousDbContext>(options =>
    options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=dummy;"));

var serviceProvider = services.BuildServiceProvider();
using var scope = serviceProvider.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<Hartonomous.Data.HartonomousDbContext>();

// Get migrations SQL
var migrator = context.GetInfrastructure().GetService<IMigrator>();
var sql = migrator!.GenerateScript(fromMigration: null, toMigration: null, options: MigrationsSqlGenerationOptions.Idempotent);

Console.WriteLine($"Generated SQL script ({sql.Length} characters)\n");

// Extract CREATE TABLE statements
var tablePattern = new Regex(@"CREATE\s+TABLE\s+\[([^\]]+)\]\.\[([^\]]+)\]\s*\((.*?)\);", RegexOptions.Singleline | RegexOptions.IgnoreCase);
var matches = tablePattern.Matches(sql);

Console.WriteLine($"Found {matches.Count} CREATE TABLE statements\n");

var outputDir = Path.Combine(Environment.CurrentDirectory, "src", "Hartonomous.Database", "Tables", "Generated");
if (Directory.Exists(outputDir))
{
    foreach (var file in Directory.GetFiles(outputDir, "*.sql", SearchOption.AllDirectories))
    {
        File.Delete(file);
    }
}

foreach (Match match in matches)
{
    var schema = match.Groups[1].Value;
    var tableName = match.Groups[2].Value;
    var tableDefinition = match.Value;

    var schemaDir = Path.Combine(outputDir, schema);
    Directory.CreateDirectory(schemaDir);

    var filePath = Path.Combine(schemaDir, $"{tableName}.sql");
    var content = $@"-- Table: [{schema}].[{tableName}]
-- Generated from EF Core DbContext
-- Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

{tableDefinition}
GO
";

    File.WriteAllText(filePath, content);
    Console.WriteLine($"  Created: {schema}.{tableName}.sql");
}

Console.WriteLine($"\n=== Complete ===");
Console.WriteLine($"Output: {outputDir}");

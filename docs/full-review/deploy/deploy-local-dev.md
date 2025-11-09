# deploy/deploy-local-dev.ps1

## Purpose and Context

- PowerShell script to automate local development deployment for Hartonomous without Azure dependencies.
- Handles build, migrations, SQL CLR deployment, SQL setup scripts, optional Neo4j schema, and verification.

## Workflow Summary

1. Validates SQL Server connectivity using integrated security.
2. Builds the `Hartonomous.sln` solution in Release configuration.
3. Optionally generates/applies EF Core migrations (`dotnet ef migrations script`) unless `-SkipMigrations` is specified.
4. Enables CLR, deploys `SqlClrFunctions.dll` by embedding binary as hex, with `UNSAFE` permission set.
5. Runs helper SQL scripts (`enable-cdc.sql`, `setup-service-broker.sql`, `verify-temporal-tables.sql`).
6. Optionally applies Neo4j schema via `cypher-shell` if available and not skipped.
7. Performs basic verification (assemblies, migrations, table counts) and outputs next steps.

## Notable Details

- Converts CLR assembly to hex string in PowerShell, sending inline SQL via `sqlcmd`; lacks dependency handling for System.Memory or other supporting assemblies.
- Hard-coded Neo4j credentials (`neo4jneo4j`) and default URIs may require adjustment for real use.
- Prompts to run `dotnet ef migrations script`, assuming `Hartonomous.Data` project exists and is configured—needs validation given repo inconsistencies.

## Potential Risks / Follow-ups

- Verify that `SqlClrFunctions.dll` build artifacts exist before deployment; consider invoking a dedicated build command or wrapping with msbuild output checks.
- Assess security implications of enabling CLR with `UNSAFE` permission by default; provide guidance for safer alternatives in production.
- Add error handling around `cypher-shell` credentials and ensure passwords are not hard-coded.
- Consider parameterizing migration output path and cleaning up generated scripts after use.

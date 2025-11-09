# Hartonomous.sln

## Purpose and Context

- Primary Visual Studio solution aggregating core applications, infrastructure, databases, and test projects for the Hartonomous platform.
- Organizes projects into `src` and `tests` solution folders to align with repository structure.

## Structure and Contents

- Includes key application projects: `Hartonomous.Api`, `Hartonomous.Admin`, worker services (`Hartonomous.Workers.CesConsumer`, `Hartonomous.Workers.Neo4jSync`), database CLR, and shared libraries (`Core`, `Data`, `Infrastructure`, `Shared.Contracts`, `Core.Performance`).
- Test coverage through unit, integration, database, and end-to-end test projects under the `tests` folder.
- Solution configurations defined for Debug/Release across Any CPU/x64/x86, mapping each project to the appropriate build/platform settings.
- Nested project hierarchy ensures `src` and `tests` folders group their respective project GUIDs.

## Notable Details

- Solution GUID `{2B5B49D1-CA22-4BA2-A41C-0E5711B980D4}` and Visual Studio version 17.5.2 target modern tooling.
- All projects configured to build for Any CPU even when x64/x86 configurations exist; may limit ability to define platform-specific settings.
- Projects like `Hartonomous.Database.Clr` and worker processes indicate significant backend infrastructure beyond the API.

## Potential Risks / Follow-ups

- Ensure every project referenced here still exists in `src/` and `tests/`; discrepancies with `deleted_files.txt` should be addressed if files were removed.
- If certain projects (e.g., Admin UI or specific workers) are deprecated, remove them to reduce solution load and build times.
- Consider adding solution folders for infrastructure scripts or docs if developers frequently access them via Visual Studio.

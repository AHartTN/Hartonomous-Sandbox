# Hartonomous.Tests.sln

## Purpose and Context

- Secondary Visual Studio solution focused exclusively on the test projects for Hartonomous.
- Provides a lean environment for running and maintaining tests without loading the full application solution.

## Structure and Contents

- Groups test projects under `tests` solution folder, including unit, integration, database, and end-to-end test suites.
- Mirrors build configurations (Debug/Release, Any CPU/x64/x86) mapped to each project similarly to the main solution.
- Uses the same project GUIDs as `Hartonomous.sln`, ensuring consistent linkage across solutions.

## Notable Details

- Visual Studio version baseline slightly older (17.0.31903.59) but still within VS 2022 range; verify compatibility with current tooling.
- All platform configurations map back to Any CPU builds; no platform-specific overrides exist for tests.

## Potential Risks / Follow-ups

- Confirm the referenced test project files still exist—`deleted_files.txt` lists missing integration tests which would break this solution.
- Consider adding newer test projects (if any) to keep this solution aligned with current testing scope.

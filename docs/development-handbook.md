# Development Handbook

This handbook describes how to contribute to Hartonomous without breaking the platform.  It focuses on local environment setup, coding practices, and review expectations.

## Local Environment

1. Install the prerequisites listed in the root README (SQL Server 2025, .NET 10, Neo4j, PowerShell 7).
2. Clone the repository and restore dependencies:

   ```powershell
   git clone https://github.com/AHartTN/Hartonomous.git
   cd Hartonomous
   dotnet restore
   ```

3. Create a developer database.  Use the deployment script with `-Environment Dev` to seed sample content and a default billing rate plan.
4. Set up appsettings:
   - Copy `src/Hartonomous.Infrastructure/appsettings.Development.example.json` to `appsettings.Development.json` (create if missing).
   - Set `ConnectionStrings:HartonomousDb`, `Neo4j` credentials, and Service Broker configuration.
5. Start the core services you need (Neo4jSync worker, CesConsumer, admin UI).  For local development, you can run them in separate terminals.

## Coding Guidelines

- **Follow the architecture boundaries.**
  - Domain objects live in `Hartonomous.Core`.
  - EF Core configuration lives in `Hartonomous.Data`.
  - Concrete services/repositories belong to `Hartonomous.Infrastructure`.
- **Avoid raw SQL.** Use migrations and `DbContext` unless you are inside `SqlClr` or a performance-sensitive stored procedure.
- **Prefer constructor injection** over service locators; keep services small.
- **Log with context.** Use structured logging (`logger.LogInformation("Billing {Operation} ...", ...)`) so telemetry dashboards remain useful.
- **Keep docs fresh.** Update the relevant markdown files in this directory when behaviour changes.

## Branching & Commits

- Use short-lived feature branches named `feature/<slug>` or `fix/<slug>`.
- Squash or reorganise commits before opening a PR.  Commits should compile and represent logical chunks.
- Include migration files and delete obsolete ones when superseded.
- Run formatting and static analysis tools before pushing:

   ```powershell
   dotnet format
   dotnet build Hartonomous.sln
   ```

## Testing Expectations

> 🚧 **Current gap:** Automated tests exist but coverage for the new billing and messaging code is low.  Every new feature should bring fresh tests.

- **Unit tests** belong in `Hartonomous.Core.Tests` and `Hartonomous.Infrastructure.Tests`.
- **Integration tests** live under `tests/Integration.Tests` and can spin up SQL Server/Neo4j containers.
- When adding a migration, create a regression test that at least exercises the new entity configuration.
- For messaging flows, prefer black-box tests that publish to `IMessageBroker` and assert downstream effects.

Run tests with:

```powershell
dotnet test Hartonomous.sln
```

## Code Review Checklist

- [ ] Does the PR include necessary migrations and configuration updates?
- [ ] Are access policies/throttles updated when new handlers are added?
- [ ] Do logs and telemetry follow existing conventions?
- [ ] Are documents in `docs/` updated?
- [ ] Are secrets excluded from commits and configuration?

## Tooling & Automation

- **Git hooks.** Optional `.githooks/pre-commit` runs `dotnet format`. Enable via `git config core.hooksPath .githooks`.
- **CI/CD.** Pipeline runs `dotnet build`, `dotnet test`, and `dotnet ef migrations bundle` for deployment artifacts.  Add new steps in `azure-pipelines.yml` as needed.
- **Static analysis.** SonarQube (or equivalent) can be linked to surfacing technical debt. Integrate as part of review when available.

This handbook should remain concise.  If guidelines grow too heavy, break them into dedicated docs and link from here.

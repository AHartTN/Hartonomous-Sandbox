# Contributing to Hartonomous

Thank you for your interest in contributing to Hartonomous! This document provides guidelines for contributing to the project.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [How to Contribute](#how-to-contribute)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Testing Requirements](#testing-requirements)
- [Documentation](#documentation)
- [Submitting Changes](#submitting-changes)

---

## Code of Conduct

We are committed to providing a welcoming and inclusive environment. Please be respectful and professional in all interactions.

---

## Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```powershell
   git clone https://github.com/YOUR_USERNAME/Hartonomous-Sandbox.git
   cd Hartonomous-Sandbox
   ```
3. **Set up the development environment** - see [README.md](README.md#prerequisites)
4. **Create a branch** for your changes:
   ```powershell
   git checkout -b feature/your-feature-name
   ```

---

## How to Contribute

### Reporting Bugs

- Use the [GitHub Issues](https://github.com/AHartTN/Hartonomous-Sandbox/issues) tracker
- Check if the issue already exists
- Provide detailed reproduction steps
- Include relevant logs and error messages
- Specify your environment (OS, SQL Server version, .NET version)

### Suggesting Features

- Use [GitHub Discussions](https://github.com/AHartTN/Hartonomous-Sandbox/discussions) for feature ideas
- Clearly describe the use case and benefits
- Consider implementation implications
- Be open to feedback and alternative approaches

### Contributing Code

1. Check existing issues and pull requests
2. Discuss major changes in an issue first
3. Follow the development workflow below
4. Ensure all tests pass
5. Submit a pull request

---

## Development Workflow

### 1. Build the Project

```powershell
# Build the solution
dotnet build

# Build the DACPAC
msbuild src/Hartonomous.Database/Hartonomous.Database.sqlproj /t:Build /p:Configuration=Release
```

### 2. Run Tests

```powershell
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Hartonomous.Core.Tests/

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### 3. Deploy Database Locally

```powershell
# Deploy to local SQL Server
.\scripts\deploy-database-unified.ps1 -Server localhost -Database Hartonomous_Dev
```

---

## Coding Standards

### C# Code Style

- Follow [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful names for variables, methods, and classes
- Add XML documentation comments for public APIs
- Keep methods focused and concise
- Use `async`/`await` for I/O operations

### SQL Code Style

- Use UPPER CASE for SQL keywords
- Use PascalCase for object names (tables, procedures, functions)
- Add header comments to stored procedures and functions
- Use schema qualifiers (e.g., `dbo.TableName`)
- Parameterize all queries to prevent SQL injection

### File Organization

```
src/
├── Hartonomous.Api/          # REST API
├── Hartonomous.Core/         # Core business logic
├── Hartonomous.Infrastructure/  # Data access, external services
├── Hartonomous.Database/     # DACPAC project (source of truth)
└── Hartonomous.Workers.*/    # Background workers

tests/
├── Hartonomous.Core.Tests/  # Unit tests
├── Hartonomous.IntegrationTests/  # Integration tests
└── Hartonomous.Benchmarks/  # Performance tests
```

---

## Testing Requirements

### All Code Changes Must Include Tests

- **Unit tests** for business logic
- **Integration tests** for database operations
- **API tests** for controller endpoints
- **Performance tests** for critical paths

### Test Coverage Requirements

- New code: **minimum 80% coverage**
- Critical paths: **minimum 95% coverage**
- All tests must pass before submitting PR

See [Testing Guide](docs/development/testing-guide.md) for detailed testing standards.

---

## Documentation

### Documentation Requirements

- Update relevant `.md` files in `docs/` directory
- Add XML comments to public APIs
- Update README.md if changing setup or prerequisites
- Document breaking changes in PR description
- Update CHANGELOG.md (when it exists)

### Documentation Structure

All documentation lives in the `docs/` directory:

```
docs/
├── README.md                    # Documentation index
├── architecture/               # Architecture documentation
├── deployment/                 # Deployment guides
├── development/                # Development guides
├── api/                        # API documentation
├── security/                   # Security documentation
└── reference/                  # Technical references
```

---

## Submitting Changes

### Pull Request Process

1. **Update your branch** with latest main:
   ```powershell
   git checkout main
   git pull upstream main
   git checkout your-branch
   git rebase main
   ```

2. **Run all tests** locally:
   ```powershell
   dotnet test
   ```

3. **Commit your changes** with clear messages:
   ```powershell
   git commit -m "feat: add semantic search caching
   
   - Implement LRU cache for embedding results
   - Add cache invalidation on model updates
   - Include cache hit/miss metrics"
   ```

4. **Push to your fork**:
   ```powershell
   git push origin your-branch
   ```

5. **Create a pull request** on GitHub:
   - Provide clear title and description
   - Reference related issues
   - Describe testing performed
   - Note any breaking changes

### Commit Message Format

Use conventional commits:

- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation changes
- `test:` - Test additions or changes
- `refactor:` - Code refactoring
- `perf:` - Performance improvements
- `chore:` - Maintenance tasks

### PR Review Checklist

Before submitting, ensure:

- [ ] All tests pass
- [ ] Code follows style guidelines
- [ ] Documentation is updated
- [ ] Commit messages are clear
- [ ] No unnecessary files included
- [ ] Breaking changes are documented
- [ ] Performance impact considered

---

## Database Changes

### DACPAC-First Development

**Important**: The SQL Server Database Project (`src/Hartonomous.Database/`) is the source of truth for schema.

- Make schema changes in `.sql` files, not in EF Core migrations
- Build the DACPAC after schema changes
- Deploy the DACPAC to test environments
- Update EF Core models to match (use reverse engineering if needed)

### Schema Change Process

1. Update `.sql` files in `src/Hartonomous.Database/`
2. Build DACPAC:
   ```powershell
   msbuild src/Hartonomous.Database/Hartonomous.Database.sqlproj /t:Build
   ```
3. Deploy to test database:
   ```powershell
   .\scripts\deploy-database-unified.ps1 -Server localhost -Database Hartonomous_Test
   ```
4. Verify changes with integration tests
5. Update EF Core models if needed

---

## CLR Assembly Development

### CLR Security Requirements

- All CLR assemblies use **UNSAFE** permission set
- Code must be reviewed for security implications
- Follow principles in [CLR Security Documentation](docs/security/clr-security-analysis.md)
- Test CLR functions thoroughly with integration tests

### CLR Deployment

CLR assemblies are deployed using:
```powershell
.\scripts\deploy-clr-secure.ps1
```

See [CLR Deployment Guide](docs/deployment/clr-deployment.md) for details.

---

## Getting Help

- **Documentation**: Check [docs/](docs/) directory
- **Issues**: Search [existing issues](https://github.com/AHartTN/Hartonomous-Sandbox/issues)
- **Discussions**: Ask in [GitHub Discussions](https://github.com/AHartTN/Hartonomous-Sandbox/discussions)

---

## License

By contributing, you agree that your contributions will be licensed under the same license as the project. See [LICENSE](LICENSE) for details.

---

Thank you for contributing to Hartonomous! 🚀

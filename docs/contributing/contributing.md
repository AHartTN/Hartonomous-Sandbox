# Contributing to Hartonomous

Thank you for your interest in contributing to **Hartonomous**! This document provides guidelines and information for contributors.

---

## Table of Contents

1. [Code of Conduct](#code-of-conduct)
2. [Getting Started](#getting-started)
3. [Development Workflow](#development-workflow)
4. [Pull Request Process](#pull-request-process)
5. [Issue Guidelines](#issue-guidelines)
6. [Testing Requirements](#testing-requirements)
7. [Documentation Standards](#documentation-standards)
8. [Community](#community)

---

## Code of Conduct

### Our Pledge

We are committed to providing a welcoming and inclusive environment for all contributors, regardless of experience level, gender, gender identity, sexual orientation, disability, personal appearance, body size, race, ethnicity, age, religion, or nationality.

### Expected Behavior

- Use welcoming and inclusive language
- Be respectful of differing viewpoints and experiences
- Gracefully accept constructive criticism
- Focus on what is best for the community
- Show empathy towards other community members

### Unacceptable Behavior

- Harassment, discrimination, or intimidation of any kind
- Trolling, insulting/derogatory comments, and personal attacks
- Public or private harassment
- Publishing others' private information without permission
- Other conduct which could reasonably be considered inappropriate

### Enforcement

Violations of the Code of Conduct may result in:

1. Warning from project maintainers
2. Temporary ban from project spaces
3. Permanent ban from the project

Report violations to: conduct@hartonomous.ai

---

## Getting Started

### Prerequisites

**Required Tools**:

- .NET 10 SDK
- SQL Server 2025 Developer Edition
- Neo4j 5.x (Community or Enterprise)
- PowerShell 7+
- Git

**Optional Tools**:

- Visual Studio 2022 or VS Code
- Azure CLI (for deployment)
- Docker Desktop (for containerized development)

See [Development Setup](development-setup.md) for detailed installation instructions.

### Fork and Clone

1. **Fork** the repository on GitHub
2. **Clone** your fork locally:

```bash
git clone https://github.com/YOUR-USERNAME/Hartonomous.git
cd Hartonomous
```

3. **Add upstream** remote:

```bash
git remote add upstream https://github.com/hartonomous/Hartonomous.git
```

4. **Verify** remotes:

```bash
git remote -v
# origin    https://github.com/YOUR-USERNAME/Hartonomous.git (fetch)
# origin    https://github.com/YOUR-USERNAME/Hartonomous.git (push)
# upstream  https://github.com/hartonomous/Hartonomous.git (fetch)
# upstream  https://github.com/hartonomous/Hartonomous.git (push)
```

### Build the Project

```powershell
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test
```

---

## Development Workflow

### Branching Strategy

We use **Git Flow** with these primary branches:

- **`main`**: Production-ready code
- **`develop`**: Integration branch for features
- **`feature/*`**: New features
- **`bugfix/*`**: Bug fixes
- **`hotfix/*`**: Urgent production fixes
- **`release/*`**: Release preparation

### Creating a Feature Branch

```bash
# Update develop branch
git checkout develop
git pull upstream develop

# Create feature branch
git checkout -b feature/your-feature-name

# Make changes and commit
git add .
git commit -m "feat: add new feature description"

# Push to your fork
git push origin feature/your-feature-name
```

### Commit Message Format

We follow **Conventional Commits** specification:

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types**:

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, no logic changes)
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `test`: Adding or updating tests
- `build`: Build system changes
- `ci`: CI/CD pipeline changes
- `chore`: Maintenance tasks

**Examples**:

```bash
feat(api): add semantic search endpoint

Implements /api/query/semantic endpoint with support for:
- Text query embedding
- Top-K results
- Spatial pre-filtering
- Result deduplication

Closes #123
```

```bash
fix(database): resolve deadlock in atom insertion

Added READPAST hint to prevent blocking on concurrent inserts.
Reduces deadlock frequency from 5% to <0.1% under load.

Fixes #456
```

---

## Pull Request Process

### Before Submitting

1. **Update your branch** with latest develop:

```bash
git checkout develop
git pull upstream develop
git checkout feature/your-feature-name
git rebase develop
```

2. **Run all tests**:

```bash
dotnet test
```

3. **Check code style**:

```bash
dotnet format
```

4. **Update documentation** if needed

5. **Add tests** for new functionality

### Submitting a Pull Request

1. **Push** your branch to your fork:

```bash
git push origin feature/your-feature-name
```

2. **Create Pull Request** on GitHub:
   - Base branch: `develop`
   - Compare branch: `feature/your-feature-name`
   - Fill out PR template completely

3. **PR Template** includes:
   - Description of changes
   - Related issue numbers
   - Type of change (feature, bugfix, etc.)
   - Checklist of completed items
   - Screenshots (if applicable)

### PR Review Process

**Automated Checks**:

- ✅ Build succeeds
- ✅ All tests pass
- ✅ Code coverage ≥ 80%
- ✅ No critical security vulnerabilities
- ✅ Code style validation

**Manual Review**:

- At least **2 approvals** required
- Maintainers review within **48 hours**
- Address review comments
- Re-request review after changes

### Merging

- **Squash and merge** for feature branches
- **Merge commit** for release branches
- Maintainers will merge after approval
- Delete branch after merge

---

## Issue Guidelines

### Creating an Issue

**Issue Types**:

1. **Bug Report**: Something isn't working correctly
2. **Feature Request**: New functionality suggestion
3. **Documentation**: Documentation improvements
4. **Question**: General questions or clarifications

### Bug Report Template

```markdown
**Describe the bug**
A clear description of the bug.

**To Reproduce**
Steps to reproduce the behavior:
1. Call endpoint '...'
2. With parameters '...'
3. See error

**Expected behavior**
What you expected to happen.

**Actual behavior**
What actually happened.

**Environment**
- OS: [e.g., Windows 11]
- .NET Version: [e.g., 10.0]
- SQL Server Version: [e.g., 2025]
- Hartonomous Version: [e.g., 1.2.0]

**Additional context**
Any other relevant information.
```

### Feature Request Template

```markdown
**Is your feature request related to a problem?**
Describe the problem you're trying to solve.

**Describe the solution you'd like**
Clear description of desired functionality.

**Describe alternatives you've considered**
Other approaches you've evaluated.

**Additional context**
Mockups, examples, or references.
```

### Issue Labels

| Label | Description |
|-------|-------------|
| `bug` | Something isn't working |
| `enhancement` | New feature or request |
| `documentation` | Documentation improvements |
| `good first issue` | Beginner-friendly |
| `help wanted` | Extra attention needed |
| `question` | Further information requested |
| `wontfix` | Will not be addressed |
| `duplicate` | Duplicate of existing issue |
| `priority: high` | High priority |
| `priority: low` | Low priority |

---

## Testing Requirements

### Test Coverage

**Minimum Coverage**: 80% overall

**Per-Component**:

- API Controllers: ≥ 90%
- Core Services: ≥ 90%
- Database Layer: ≥ 85%
- Utilities: ≥ 80%

### Test Types

#### 1. Unit Tests

Location: `tests/Hartonomous.UnitTests/`

```csharp
[Fact]
public async Task SemanticSearch_WithValidQuery_ReturnsResults()
{
    // Arrange
    var mockService = new Mock<IQueryService>();
    var controller = new QueryController(mockService.Object);
    var request = new SemanticSearchRequest
    {
        Query = "test query",
        TopK = 10
    };

    // Act
    var result = await controller.SemanticSearch(request);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(10, result.Results.Count);
}
```

#### 2. Integration Tests

Location: `tests/Hartonomous.IntegrationTests/`

```csharp
[Fact]
public async Task DataIngestion_E2E_CreatesAtoms()
{
    // Arrange
    using var factory = new WebApplicationFactory<Program>();
    var client = factory.CreateClient();

    // Act
    var content = new MultipartFormDataContent();
    content.Add(new StringContent("test content"), "file", "test.txt");
    
    var response = await client.PostAsync("/api/ingestion/file", content);

    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<IngestionResult>();
    Assert.True(result.AtomsCreated > 0);
}
```

#### 3. End-to-End Tests

Location: `tests/Hartonomous.EndToEndTests/`

```csharp
[Fact]
public async Task CompleteWorkflow_IngestQueryReason_Success()
{
    // 1. Ingest data
    var ingestionResult = await IngestTestData();
    
    // 2. Query atoms
    var queryResult = await QueryAtoms("test query");
    
    // 3. Run reasoning
    var reasoningResult = await RunChainOfThought(queryResult.Results);
    
    // Assert complete workflow
    Assert.True(reasoningResult.TotalConfidence > 0.8);
}
```

#### 4. Database Tests

Location: `tests/Hartonomous.DatabaseTests/`

```csharp
[Fact]
public async Task AtomBulkInsert_1000Atoms_CompletesUnder500ms()
{
    // Arrange
    var atoms = GenerateTestAtoms(1000);
    var stopwatch = Stopwatch.StartNew();

    // Act
    await atomBulkInsertService.BulkInsertAsync(atoms);
    stopwatch.Stop();

    // Assert
    Assert.True(stopwatch.ElapsedMilliseconds < 500);
}
```

### Running Tests

```powershell
# All tests
dotnet test

# Specific test project
dotnet test tests/Hartonomous.UnitTests

# With coverage
dotnet test --collect:"XPlat Code Coverage"

# Filter by category
dotnet test --filter Category=Integration
```

---

## Documentation Standards

### Code Documentation

**XML Comments** for public APIs:

```csharp
/// <summary>
/// Performs semantic search across atoms.
/// </summary>
/// <param name="request">Search request with query and filters.</param>
/// <returns>Search results with top-K atoms.</returns>
/// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
[HttpPost("semantic")]
public async Task<SemanticSearchResponse> SemanticSearch([FromBody] SemanticSearchRequest request)
{
    // Implementation
}
```

**SQL Comments** for stored procedures:

```sql
-- =============================================
-- Description: Performs A* pathfinding in 3D semantic space
-- Parameters:
--   @StartAtomId: Starting atom ID
--   @GoalX, @GoalY, @GoalZ: Goal coordinates
--   @MaxPathLength: Maximum path hops
-- Returns: Path as table of atoms with costs
-- =============================================
CREATE PROCEDURE dbo.sp_SpatialAStar
    @StartAtomId INT,
    @GoalX FLOAT,
    @GoalY FLOAT,
    @GoalZ FLOAT,
    @MaxPathLength INT = 10
AS
BEGIN
    -- Implementation
END
```

### Markdown Documentation

**File Structure**:

- Headings: H1 for title, H2 for sections, H3 for subsections
- Code blocks: Use language-specific syntax highlighting
- Tables: Use for structured data
- Links: Use relative links for internal docs

**Example**:

```markdown
# Feature Name

Brief description of the feature.

## Usage

### Basic Example

\`\`\`csharp
var client = new HartonomousClient();
var result = await client.DoSomething();
\`\`\`

### Advanced Example

\`\`\`csharp
var result = await client.DoSomethingAdvanced(new Options
{
    Setting1 = true,
    Setting2 = 42
});
\`\`\`

## Configuration

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `setting1` | bool | false | Enable feature |
| `setting2` | int | 10 | Timeout in seconds |
```

### API Documentation

All new endpoints must include:

- Description and purpose
- Authentication requirements
- Request/response examples
- Error codes and handling
- SDK examples (C#, Python, TypeScript)
- Performance characteristics

See [API Reference](../api/README.md) for examples.

---

## Community

### Communication Channels

- **GitHub Discussions**: General discussions, Q&A
- **GitHub Issues**: Bug reports, feature requests
- **Discord**: Real-time chat (link in README)
- **Email**: conduct@hartonomous.ai (Code of Conduct violations)

### Getting Help

1. **Search existing issues** first
2. **Check documentation** at docs/
3. **Ask in GitHub Discussions** for general questions
4. **Create an issue** for bugs or feature requests

### Recognition

Contributors are recognized in:

- **CONTRIBUTORS.md**: All contributors listed
- **Release Notes**: Contributions highlighted
- **GitHub Insights**: Automatic contribution tracking

---

## Additional Resources

- [Development Setup](development-setup.md) - Local development environment
- [Code Standards](code-standards.md) - Coding conventions and best practices
- [Pull Request Guide](pull-requests.md) - Detailed PR process
- [Architecture Documentation](../architecture/) - System architecture
- [API Reference](../api/) - Complete API documentation

---

## License

By contributing to Hartonomous, you agree that your contributions will be licensed under the [MIT License](../../LICENSE).

---

## Questions?

If you have questions not covered in this guide:

- Open a [GitHub Discussion](https://github.com/hartonomous/Hartonomous/discussions)
- Check the [FAQ](../getting-started/faq.md)
- Reach out on [Discord](https://discord.gg/hartonomous)

Thank you for contributing to Hartonomous! 🚀

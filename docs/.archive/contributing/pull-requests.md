# Pull Request Guide

This guide provides detailed information about creating, reviewing, and merging pull requests for Hartonomous.

---

## Table of Contents

1. [Before You Start](#before-you-start)
2. [Creating a Pull Request](#creating-a-pull-request)
3. [PR Template](#pr-template)
4. [Review Process](#review-process)
5. [CI/CD Checks](#cicd-checks)
6. [Addressing Feedback](#addressing-feedback)
7. [Merging](#merging)

---

## Before You Start

### Prerequisites

✅ **Issue Exists**: Create or find an issue describing the problem/feature  
✅ **Branch Created**: Create feature branch from `develop`  
✅ **Code Complete**: All changes implemented and tested locally  
✅ **Tests Pass**: All unit, integration, and E2E tests pass  
✅ **Code Formatted**: Run `dotnet format` to fix style issues  
✅ **Documentation Updated**: Update relevant documentation  

### Update Your Branch

Before creating a PR, update your branch with latest `develop`:

```bash
# Switch to develop and pull latest
git checkout develop
git pull upstream develop

# Switch to your feature branch
git checkout feature/your-feature-name

# Rebase on develop
git rebase develop

# If there are conflicts, resolve them and continue
git add .
git rebase --continue

# Force push to your fork (rebase rewrites history)
git push origin feature/your-feature-name --force
```

---

## Creating a Pull Request

### 1. Push Your Branch

```bash
git push origin feature/your-feature-name
```

### 2. Open PR on GitHub

1. Navigate to [https://github.com/hartonomous/Hartonomous](https://github.com/hartonomous/Hartonomous)
2. Click **"Pull requests"** tab
3. Click **"New pull request"**
4. Select branches:
   - **Base**: `develop`
   - **Compare**: `your-username:feature/your-feature-name`
5. Click **"Create pull request"**

### 3. Fill Out PR Template

See [PR Template](#pr-template) below for complete template.

### 4. Link Related Issues

In the PR description, reference related issues:

```markdown
Closes #123
Fixes #456
Related to #789
```

**GitHub will automatically close linked issues when PR is merged.**

---

## PR Template

### Standard Template

When creating a PR, fill out this template:

```markdown
## Description

Brief description of what this PR does.

## Related Issue(s)

Closes #123

## Type of Change

- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update
- [ ] Performance improvement
- [ ] Code refactoring
- [ ] CI/CD pipeline change

## Changes Made

- Added semantic search endpoint (`/api/query/semantic`)
- Implemented spatial pre-filtering using R-Tree index
- Added deduplication for identical atoms
- Created unit tests for `SemanticSearchService`
- Updated API documentation with examples

## Testing

### Unit Tests

```bash
dotnet test tests/Hartonomous.UnitTests --filter "FullyQualifiedName~SemanticSearch"
```

**Results**: 15 tests passed, 0 failed

### Integration Tests

```bash
dotnet test tests/Hartonomous.IntegrationTests --filter "Category=SemanticSearch"
```

**Results**: 8 tests passed, 0 failed

### Manual Testing

Tested semantic search with:
- Text queries (10,000 atoms indexed)
- Filters (modality, source type, date range)
- Pagination (100 results across 5 pages)

**Performance**: Average query time: 22ms

## Screenshots (if applicable)

_Add screenshots of UI changes, API responses, etc._

## Checklist

- [x] My code follows the code standards of this project
- [x] I have performed a self-review of my own code
- [x] I have commented my code, particularly in hard-to-understand areas
- [x] I have made corresponding changes to the documentation
- [x] My changes generate no new warnings
- [x] I have added tests that prove my fix is effective or that my feature works
- [x] New and existing unit tests pass locally with my changes
- [x] Any dependent changes have been merged and published in downstream modules

## Additional Notes

This implementation uses the Semantic-First Architecture pattern (O(log N) spatial pre-filter + O(K·D) vector similarity). Expected performance: <25ms for queries against 3.5B atoms.
```

---

## Review Process

### Automated Checks

PR must pass all automated checks before review:

#### 1. Build Validation

```yaml
# .github/workflows/pr-validation.yml
- name: Build Solution
  run: dotnet build --configuration Release
```

**Status**: ✅ Build successful or ❌ Build failed

#### 2. Unit Tests

```yaml
- name: Run Unit Tests
  run: dotnet test tests/Hartonomous.UnitTests --no-build
```

**Status**: ✅ 247 tests passed or ❌ 3 tests failed

#### 3. Integration Tests

```yaml
- name: Run Integration Tests
  run: dotnet test tests/Hartonomous.IntegrationTests --no-build
```

**Status**: ✅ 89 tests passed

#### 4. Code Coverage

```yaml
- name: Code Coverage
  run: dotnet test --collect:"XPlat Code Coverage"
```

**Requirement**: ≥ 80% overall coverage

**Status**: ✅ 87% coverage or ❌ 72% coverage (below threshold)

#### 5. Code Style

```yaml
- name: Check Code Style
  run: dotnet format --verify-no-changes
```

**Status**: ✅ No formatting issues or ❌ 12 files need formatting

#### 6. Security Scan

```yaml
- name: Security Scan
  run: dotnet list package --vulnerable --include-transitive
```

**Status**: ✅ No vulnerabilities or ⚠️ 2 moderate vulnerabilities

### Manual Review

**Reviewers**: At least **2 approvals** required (1 maintainer + 1 contributor)

**Review Checklist**:

- [ ] Code follows [code standards](code-standards.md)
- [ ] Changes are well-documented
- [ ] Tests adequately cover new functionality
- [ ] No unnecessary complexity
- [ ] Performance considerations addressed
- [ ] Security implications evaluated
- [ ] Breaking changes clearly documented

### Review Timeline

**Initial Review**: Within **48 hours** of PR creation  
**Follow-up Reviews**: Within **24 hours** of updates  

---

## CI/CD Checks

### GitHub Actions Workflows

#### PR Validation

Runs on every PR:

1. **Build**: Compile solution in Release mode
2. **Unit Tests**: Run all unit tests
3. **Integration Tests**: Run integration tests against test database
4. **Code Coverage**: Generate coverage report
5. **Code Style**: Verify formatting
6. **Security Scan**: Check for vulnerabilities

**Configuration**: `.github/workflows/pr-validation.yml`

#### Database Tests

Runs on PRs affecting database:

1. **Build DACPAC**: Compile database project
2. **Deploy to Test DB**: Deploy to SQL Server test instance
3. **Run Database Tests**: Execute database-specific tests
4. **Performance Tests**: Validate query performance

**Triggers**:

```yaml
on:
  pull_request:
    paths:
      - 'src/Hartonomous.Database/**'
      - 'src/Hartonomous.Clr/**'
      - 'tests/Hartonomous.DatabaseTests/**'
```

### Local Pre-PR Validation

Run these checks locally before creating PR:

```powershell
# 1. Build solution
dotnet build --configuration Release

# 2. Run all tests
dotnet test

# 3. Check code coverage
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report

# 4. Format code
dotnet format

# 5. Security scan
dotnet list package --vulnerable --include-transitive
```

---

## Addressing Feedback

### Responding to Comments

**Be Responsive**:

- Respond to all review comments
- Mark resolved comments as "Resolved"
- Explain decisions if you disagree with feedback

**Be Respectful**:

```markdown
✅ Good response:
"Good catch! I'll refactor this to use dependency injection instead of static methods."

❌ Bad response:
"This works fine, no need to change it."
```

### Making Changes

**Small Changes**: Commit directly to PR branch

```bash
# Make changes
git add .
git commit -m "refactor: use dependency injection in AtomService"
git push origin feature/your-feature-name
```

**Large Changes**: Consider creating new commits for clarity

```bash
# Commit 1: Address review feedback
git commit -m "refactor: extract business logic to service layer"

# Commit 2: Add tests
git commit -m "test: add unit tests for AtomService"

# Push all commits
git push origin feature/your-feature-name
```

### Re-Requesting Review

After addressing feedback:

1. **Respond to Comments**: Reply to each comment explaining changes
2. **Resolve Conversations**: Mark conversations as resolved
3. **Re-Request Review**: Click "Re-request review" button on GitHub

**Template Response**:

```markdown
Fixed in commit abc1234. Updated the service to use dependency injection and added corresponding unit tests.
```

---

## Merging

### Merge Requirements

Before merging, PR must satisfy:

✅ **All CI checks pass**  
✅ **2+ approvals** (1 maintainer minimum)  
✅ **No unresolved conversations**  
✅ **Up-to-date with base branch** (`develop`)  
✅ **No merge conflicts**  

### Merge Strategies

#### Squash and Merge (Default for Features)

**Use For**: Feature branches, bug fixes

**Result**: All commits squashed into single commit on `develop`

**Commit Message Format**:

```
feat(api): add semantic search endpoint (#123)

Implements /api/query/semantic with spatial pre-filtering.
Supports text queries with Top-K results and deduplication.

Changes:
- Added SemanticSearchService
- Created spatial pre-filter logic
- Implemented deduplication
- Added comprehensive unit tests

Co-authored-by: Contributor Name <contributor@example.com>
```

**Benefits**:

- Clean, linear history
- Easy to revert entire feature
- Easier to read git log

#### Merge Commit (For Release Branches)

**Use For**: `release/*` branches merging to `main`

**Result**: Merge commit preserves branch history

**Benefits**:

- Preserve complete history
- Clear merge points

#### Rebase and Merge (Rarely Used)

**Use For**: Simple, single-commit PRs

**Result**: Commits rebased onto base branch

**Benefits**:

- Linear history
- No merge commits

### Who Merges

**Maintainers Only**: Only project maintainers can merge PRs

**Process**:

1. Maintainer verifies all requirements met
2. Maintainer selects merge strategy
3. Maintainer clicks "Squash and merge" (or appropriate button)
4. Maintainer confirms commit message
5. PR merged to `develop`

### After Merge

**Automatic Actions**:

1. ✅ PR closed
2. ✅ Linked issues closed (if using "Closes #123" syntax)
3. ✅ Branch marked as merged
4. ✅ CI/CD pipeline runs on `develop`

**Manual Actions**:

1. **Delete Branch**: Click "Delete branch" button on PR page
2. **Update Local Repo**:

```bash
# Switch to develop
git checkout develop

# Pull latest (includes your merged PR)
git pull upstream develop

# Delete local feature branch
git branch -d feature/your-feature-name

# Delete remote feature branch (if not done via GitHub UI)
git push origin --delete feature/your-feature-name
```

---

## PR Examples

### Good PR Example

**Title**: `feat(api): add semantic search with spatial pre-filtering`

**Description**:

```markdown
## Description

Implements semantic search endpoint with O(log N) spatial pre-filtering for sub-25ms query performance on 3.5B atoms.

## Related Issue(s)

Closes #123

## Type of Change

- [x] New feature

## Changes Made

- Added `/api/query/semantic` endpoint
- Implemented R-Tree spatial pre-filter (O(log N))
- Added vector similarity ranking (O(K·D))
- Created deduplication logic
- Added comprehensive tests (90% coverage)
- Updated API documentation with examples

## Testing

### Unit Tests (15 passed)
- SemanticSearchService.Search_ValidQuery_ReturnsResults
- SemanticSearchService.Search_WithFilters_AppliesFilters
- ...

### Integration Tests (8 passed)
- E2E test: Ingest 10,000 atoms → Query → Verify results

### Performance Test
- 10,000 atoms: Avg 18ms, P95 22ms, P99 28ms ✅

## Screenshots

API Response Example:
```json
{
  "results": [...],
  "totalResults": 10,
  "queryTimeMs": 18
}
```

## Checklist

- [x] Code follows standards
- [x] Self-reviewed
- [x] Documentation updated
- [x] Tests added
- [x] All tests pass
```

**Result**: ✅ Approved and merged quickly

### Bad PR Example

**Title**: `Update code`

**Description**:

```markdown
Fixed some stuff
```

**Issues**:

- ❌ Vague title and description
- ❌ No linked issue
- ❌ No type of change specified
- ❌ No testing information
- ❌ Checklist not filled out

**Result**: ❌ Rejected, asked to resubmit with proper template

---

## Questions?

If you have questions about the PR process:

- Check [Contributing Guide](contributing.md)
- Review existing PRs for examples
- Ask in [GitHub Discussions](https://github.com/hartonomous/Hartonomous/discussions)
- Reach out on [Discord](https://discord.gg/hartonomous)

---

## Summary Checklist

Before creating your PR, verify:

- [ ] Code is complete and tested locally
- [ ] Branch is up-to-date with `develop`
- [ ] All tests pass (`dotnet test`)
- [ ] Code is formatted (`dotnet format`)
- [ ] Documentation updated
- [ ] PR template filled out completely
- [ ] Related issues linked
- [ ] Ready for review

Thank you for contributing to Hartonomous! 🚀

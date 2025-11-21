# ?? **HARTONOMOUS TESTING DOCUMENTATION**

**Last Updated**: January 2025 (Phase 7 Completion)  
**Status**: ?? **Partial Coverage - Known Failures Documented**

---

## **?? QUICK SUMMARY**

| Test Suite | Status | Passed | Failed | Total | Notes |
|------------|--------|--------|--------|-------|-------|
| **Unit Tests** | ?? Mostly Passing | 119 | 15 | 134 | 89% pass rate |
| **Database Tests** | ? All Failing | 0 | 3 | 3 | Requires Docker/SQL Server |
| **Integration Tests** | ?? Not Run | - | - | - | Deferred |
| **E2E Tests** | ?? Not Run | - | - | - | Deferred |
| **TOTAL** | ?? | 119 | 18 | 137 | 87% overall |

---

## **?? HOW TO RUN TESTS**

### **Quick Validation (Core Tests)**
```powershell
# Run from repository root
.\scripts\Run-CoreTests.ps1
```

### **Individual Test Suites**

#### **Unit Tests** (Fast - ~8 seconds)
```powershell
dotnet test tests/Hartonomous.UnitTests
```
- **Purpose**: Test business logic in isolation
- **Dependencies**: None (pure unit tests)
- **Status**: 119/134 passing (89%)

#### **Database Tests** (Slow - requires infrastructure)
```powershell
dotnet test tests/Hartonomous.DatabaseTests
```
- **Purpose**: Test database integration (SQL Server, Testcontainers)
- **Dependencies**: Docker Desktop (for Testcontainers)
- **Status**: 0/3 passing (requires Docker)

---

## **?? TEST PROJECT DETAILS**

### **1. Hartonomous.UnitTests** (`tests/Hartonomous.UnitTests/`)

**Purpose**: Fast, isolated tests of core business logic

**Test Categories**:
- `Tests/Core/` - Domain models and value objects
- `Tests/Infrastructure/Services/` - Service implementations
- `Tests/Api/Controllers/` - API controller logic

**Known Failures** (15 tests):
- Likely due to missing mocks or updated interfaces
- **Action Required**: Review failures, update mocks

**Run Command**:
```powershell
dotnet test tests/Hartonomous.UnitTests --verbosity normal
```

---

### **2. Hartonomous.DatabaseTests** (`tests/Hartonomous.DatabaseTests/`)

**Purpose**: Integration tests with real SQL Server via Testcontainers

**Test Categories**:
- `Tests/Infrastructure/` - Database connectivity tests

**Known Failures** (3 tests):
- All tests fail without Docker Desktop running
- Testcontainers requires Docker to spin up SQL Server 2022

**Dependencies**:
```powershell
# Install Docker Desktop
# https://www.docker.com/products/docker-desktop/

# Verify Docker is running
docker ps
```

**Run Command**:
```powershell
# Ensure Docker Desktop is running first
dotnet test tests/Hartonomous.DatabaseTests --verbosity normal
```

**Sample Test**:
```csharp
[Fact]
public async Task Connection_CanConnectToDatabase()
{
    var connectionString = _sqlContainer.GetConnectionString();
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    
    connection.State.Should().Be(ConnectionState.Open);
}
```

---

### **3. Hartonomous.IntegrationTests** (`tests/Hartonomous.IntegrationTests/`)

**Purpose**: Test interactions between multiple services

**Status**: ?? **Not currently run** - Deferred to future phases

**Planned Coverage**:
- API ? Infrastructure ? Database workflows
- Neo4j graph operations
- Background job processing
- Service Bus messaging

---

### **4. Hartonomous.EndToEndTests** (`tests/Hartonomous.EndToEndTests/`)

**Purpose**: Full system tests with Playwright

**Status**: ?? **Not currently run** - Deferred to future phases

**Planned Coverage**:
- API endpoints via HTTP
- UI workflows (if applicable)
- Performance benchmarks

---

## **? DELETED TEST PROJECTS**

### **~~Hartonomous.Clr.Tests~~ (DELETED in Phase 7)**

**Why Deleted**:
- **Architecturally broken** - Can't unit test SQL CLR outside SQL Server
- **Missing dependencies** - Required types (TensorInfo, etc.) not available in test context
- **Wrong location** - Was in `src/` instead of `tests/`
- **Wrong approach** - CLR should be tested via DatabaseTests with real SQL Server

**Replacement Strategy**:
```csharp
// Instead of unit testing CLR functions directly, test via SQL:
[Fact]
public async Task VectorDistance_Cosine_CalculatesCorrectly()
{
    var command = connection.CreateCommand();
    command.CommandText = @"
        SELECT dbo.clr_VectorDistance(
            @vec1, @vec2, 'cosine'
        ) AS Distance";
    
    var result = await command.ExecuteScalarAsync();
    // Assert result...
}
```

---

## **?? TEST INFRASTRUCTURE**

### **Testing Stack**
- **Framework**: xUnit 2.9.2
- **Assertions**: FluentAssertions 7.0.0
- **Mocking**: NSubstitute 5.3.0
- **Database**: Testcontainers.MsSql 3.9.0
- **Coverage**: coverlet.collector 6.0.2

### **Central Package Management**
All package versions managed in `Directory.Packages.props`:
```xml
<PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
</PropertyGroup>
```

**?? Important**: Do NOT add `Version=""` attributes to `<PackageReference>` in test projects!

---

## **?? CURRENT TEST STRATEGY**

### **Phase 7 Goal**: Basic Validation ?
- ? Solution builds without errors
- ? Core unit tests run (89% passing)
- ?? Database tests documented (require Docker)
- ? Test documentation created

### **Post-Phase 7**: Incremental Improvement
1. **Fix 15 failing unit tests** - Update mocks for new interfaces
2. **Run database tests locally** - Verify Docker + Testcontainers setup
3. **Add test categories** - `[Trait("Category", "Fast")]` for CI/CD
4. **Measure coverage** - Establish baseline with coverlet
5. **CI/CD integration** - GitHub Actions workflow

---

## **?? TROUBLESHOOTING**

### **Problem: "Package version cannot be specified" error**
**Solution**: Using Central Package Management - remove `Version=""` from PackageReference

### **Problem: "xUnit types not found" error**
**Solution**: Add `using Xunit;` to test files

### **Problem: Database tests fail**
**Solution**: 
1. Install Docker Desktop
2. Start Docker
3. Run `docker ps` to verify
4. Re-run tests

### **Problem: Tests don't discover**
**Solution**:
```powershell
# Clean and rebuild
dotnet clean
dotnet build
dotnet test --list-tests
```

---

## **?? TESTING BEST PRACTICES**

### **Unit Test Naming**
```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    // Act
    // Assert
}
```

### **Test Categories**
```csharp
[Trait("Category", "Unit")]
[Trait("Category", "Fast")]
public async Task MyTest() { }
```

### **Integration Tests**
```csharp
[Trait("Category", "Integration")]
[Trait("Category", "Database")]
public async Task MyDatabaseTest() { }
```

---

## **?? RUNNING TESTS IN CI/CD**

### **GitHub Actions (Future)**
```yaml
name: Tests

on: [push, pull_request]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      - run: dotnet test tests/Hartonomous.UnitTests
  
  integration-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
      - run: dotnet test tests/Hartonomous.DatabaseTests
```

---

## **?? TEST COVERAGE GOALS**

| Phase | Target | Current | Status |
|-------|--------|---------|--------|
| **Phase 7** | Solution builds, core tests run | 89% unit | ? |
| **Phase 8** | All unit tests pass | 89% unit | ? |
| **Phase 9** | Database tests pass | 0% db | ? |
| **Phase 10** | 70% overall coverage | TBD | ? |

---

## **?? RELATED DOCUMENTATION**

- `docs/PHASE_7_HONEST_REPORT.md` - Phase 7 completion status
- `docs/TESTING_STRATEGY_PROPOSAL.md` - Full testing strategy
- `tests/Phase_5_Verification.sql` - SQL-based deployment verification
- `scripts/Run-CoreTests.ps1` - Quick test execution script

---

## **? VALIDATION CHECKLIST**

Before considering testing "complete":
- [x] Solution builds without errors
- [x] Test projects build
- [x] Unit tests run (even with failures)
- [ ] All unit tests pass
- [ ] Database tests run with Docker
- [ ] Integration tests configured
- [ ] Coverage measurement enabled
- [ ] CI/CD pipeline created

**Current Status**: 4/8 complete (50%)

---

**Questions or issues? Check `docs/TESTING_STRATEGY_PROPOSAL.md` for detailed strategy.**

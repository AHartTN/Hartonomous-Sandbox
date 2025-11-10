# SQLCLR COMPLETE REBUILD - Clean Slate Approach
**Goal**: Rebuild SqlClr from scratch, zero NuGet conflicts, clean structure

---

## WHY REBUILD vs FIX

**Current mess**:
- Old .csproj format (pre-SDK)
- Mix of `<PackageReference>` and `<Reference>` with HintPath
- System.Text.Json dependency causing version conflicts
- 52 files in flat structure
- 6 different NuGet packages with transitive dependency hell

**Rebuild benefits**:
- SDK-style project (modern, cleaner)
- Zero external dependencies except Microsoft.SqlServer.Types
- Manual JSON serialization (no System.Text.Json)
- Organized folder structure
- Builds in 30 seconds
- Deploys successfully

**Time estimate**: 4-5 hours vs days of NuGet debugging.

---

## NEW STRUCTURE (3 Projects)

### Project 1: Hartonomous.SqlClr.Core
**Purpose**: Shared utilities with ZERO dependencies

**Create**:
```bash
cd src
dotnet new classlib -n Hartonomous.SqlClr.Core -f net48
cd Hartonomous.SqlClr.Core
```

**Edit .csproj**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
  <!-- NO PackageReferences - pure C# only -->
</Project>
```

**Folder structure**:
```
Hartonomous.SqlClr.Core/
├── Math/
│   ├── VectorMath.cs           # Pure C# vector operations (no SIMD, no deps)
│   ├── StatisticalFunctions.cs # Mean, stddev, covariance, etc.
│   ├── MatrixOperations.cs     # Basic matrix math
│   └── GeometryMath.cs         # Distance calculations
├── Serialization/
│   ├── VectorParser.cs         # Parse JSON arrays to float[]
│   ├── SimpleJsonBuilder.cs    # Manual StringBuilder JSON
│   └── BinarySerializer.cs     # Byte array serialization
├── Validation/
│   ├── AtomValidator.cs        # Validate AtomId, check > 0
│   ├── SamplingValidator.cs    # Validate temperature, topK, topP
│   └── ParameterValidator.cs   # General parameter validation
└── Models/
    ├── Candidate.cs            # {AtomId, Score}
    ├── AttentionHead.cs        # Attention computation result
    └── EmbeddingVector.cs      # Wrapper for float[]
```

**SimpleJsonBuilder.cs** (replaces System.Text.Json):
```csharp
using System.Text;

namespace Hartonomous.SqlClr.Core.Serialization
{
    public static class SimpleJsonBuilder
    {
        public static string BuildObject(params (string key, object value)[] pairs)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            for (int i = 0; i < pairs.Length; i++)
            {
                if (i > 0) sb.Append(',');
                var (key, value) = pairs[i];
                sb.Append('"').Append(Escape(key)).Append("\":");
                AppendValue(sb, value);
            }
            sb.Append('}');
            return sb.ToString();
        }

        public static string BuildArray(params object[] items)
        {
            var sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < items.Length; i++)
            {
                if (i > 0) sb.Append(',');
                AppendValue(sb, items[i]);
            }
            sb.Append(']');
            return sb.ToString();
        }

        private static void AppendValue(StringBuilder sb, object value)
        {
            if (value == null)
                sb.Append("null");
            else if (value is string s)
                sb.Append('"').Append(Escape(s)).Append('"');
            else if (value is bool b)
                sb.Append(b ? "true" : "false");
            else if (value is int || value is long || value is float || value is double)
                sb.Append(value);
            else if (value is float[] arr)
                sb.Append(BuildArray(arr.Cast<object>().ToArray()));
            else
                sb.Append('"').Append(Escape(value.ToString())).Append('"');
        }

        private static string Escape(string s)
        {
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }
    }
}
```

---

### Project 2: Hartonomous.SqlClr.Functions
**Purpose**: SQL CLR assembly with functions/aggregates/UDTs

**Create**:
```bash
cd src
dotnet new classlib -n Hartonomous.SqlClr.Functions -f net48
cd Hartonomous.SqlClr.Functions
dotnet add reference ../Hartonomous.SqlClr.Core/Hartonomous.SqlClr.Core.csproj
dotnet add package Microsoft.SqlServer.Types --version 160.1000.6
```

**Edit .csproj**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <GenerateAssemblyInfo>true</GenerateAssemblyInfo>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.SqlServer.Types" Version="160.1000.6" />
    <ProjectReference Include="..\Hartonomous.SqlClr.Core\Hartonomous.SqlClr.Core.csproj" />
  </ItemGroup>
</Project>
```

**Folder structure**:
```
Hartonomous.SqlClr.Functions/
├── Aggregates/
│   ├── Neural/
│   │   ├── VectorAttention.cs
│   │   ├── AutoencoderCompression.cs
│   │   ├── GradientStatistics.cs
│   │   └── CosineAnnealing.cs
│   ├── Reasoning/
│   │   ├── TreeOfThought.cs
│   │   ├── Reflexion.cs
│   │   ├── SelfConsistency.cs
│   │   └── ChainOfThought.cs
│   ├── Graph/
│   │   ├── GraphPathSummary.cs
│   │   ├── EdgeWeightedMean.cs
│   │   └── VectorDrift.cs
│   ├── TimeSeries/
│   │   ├── SequencePatterns.cs
│   │   ├── ARForecast.cs
│   │   ├── DTWDistance.cs
│   │   └── ChangePointDetection.cs
│   ├── Anomaly/
│   │   ├── IsolationForestScore.cs
│   │   ├── LocalOutlierFactor.cs
│   │   ├── DBSCANDensity.cs
│   │   └── MahalanobisOutlier.cs
│   ├── Recommender/
│   │   ├── CollaborativeFiltering.cs
│   │   ├── ContentBasedScore.cs
│   │   ├── MatrixFactorization.cs
│   │   └── DiversityScore.cs
│   ├── Dimensionality/
│   │   ├── PCAProjection.cs
│   │   ├── TSNEProjection.cs
│   │   └── RandomProjection.cs
│   ├── Advanced/
│   │   ├── VectorCentroid.cs
│   │   ├── SpatialConvexHull.cs
│   │   ├── KMeansClustering.cs
│   │   └── CovarianceMatrix.cs
│   └── Behavioral/
│       ├── UserJourney.cs
│       ├── SessionQuality.cs
│       └── ChurnPrediction.cs
├── Functions/
│   ├── Generation/
│   │   ├── AttentionGeneration.cs      # Multi-head attention
│   │   ├── GenerationFunctions.cs      # Autoregressive generation
│   │   └── MultiModalGeneration.cs     # Text/Audio/Image/Video
│   ├── Analysis/
│   │   ├── AutonomousAnalyticsTVF.cs
│   │   ├── QueryStoreAnalyzer.cs
│   │   ├── BillingLedgerAnalyzer.cs
│   │   ├── TestResultAnalyzer.cs
│   │   └── SystemAnalyzer.cs
│   ├── Spatial/
│   │   ├── LandmarkProjection.cs       # Trilateration
│   │   ├── SpatialOperations.cs        # CreateLineStringFromWeights
│   │   └── TriangulationFunctions.cs
│   ├── Autonomous/
│   │   ├── AutonomousFunctions.cs      # Complexity calculation
│   │   └── FileSystemFunctions.cs      # File I/O, git integration
│   ├── Embedding/
│   │   ├── EmbeddingFunctions.cs       # Vector generation
│   │   └── SemanticAnalysis.cs         # NLP functions
│   ├── Audio/
│   │   └── AudioProcessing.cs          # Waveform to LINESTRING
│   ├── Image/
│   │   ├── ImageProcessing.cs          # Image to point cloud
│   │   └── ImageGeneration.cs          # Diffusion patches
│   └── Concept/
│       └── ConceptDiscovery.cs         # DBSCAN clustering
├── UDTs/
│   ├── AtomicStream.cs                 # Provenance UDT
│   ├── ComponentStream.cs              # Run-length encoding
│   └── AtomicStreamFunctions.cs        # TVF for stream segments
└── Utilities/
    ├── SqlBytesInterop.cs
    ├── ModelIngestionFunctions.cs
    └── StreamOrchestrator.cs
```

---

### Project 3: Hartonomous.SqlClr.Deployment
**Purpose**: Deploy DLL to SQL Server and register functions

**Create**:
```bash
cd src
dotnet new console -n Hartonomous.SqlClr.Deployment -f net8.0
cd Hartonomous.SqlClr.Deployment
dotnet add package Microsoft.Data.SqlClient
```

**Program.cs**:
```csharp
using System;
using System.IO;
using System.Reflection;
using Microsoft.Data.SqlClient;

namespace Hartonomous.SqlClr.Deployment
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: deploy <server> <database> <dll-path> [permission-set]");
                Console.WriteLine("Example: deploy localhost Hartonomous bin/Release/net48/Hartonomous.SqlClr.Functions.dll SAFE");
                return;
            }

            string server = args[0];
            string database = args[1];
            string dllPath = args[2];
            string permissionSet = args.Length > 3 ? args[3] : "SAFE";

            if (!File.Exists(dllPath))
            {
                Console.WriteLine($"DLL not found: {dllPath}");
                return;
            }

            string connectionString = $"Server={server};Database={database};Integrated Security=true;TrustServerCertificate=true";

            try
            {
                Console.WriteLine($"Deploying to {server}/{database}...");

                using var connection = new SqlConnection(connectionString);
                connection.Open();

                // Drop existing assembly
                DropAssembly(connection);

                // Create assembly
                CreateAssembly(connection, dllPath, permissionSet);

                // Register functions
                RegisterFunctions(connection);

                // Register aggregates
                RegisterAggregates(connection);

                // Register UDTs
                RegisterUDTs(connection);

                Console.WriteLine("Deployment successful!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        static void DropAssembly(SqlConnection conn)
        {
            Console.WriteLine("Dropping existing assembly...");
            var sql = @"
                IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'HartonomousSqlClrFunctions')
                BEGIN
                    -- Drop all functions/aggregates/UDTs first
                    DECLARE @sql NVARCHAR(MAX) = '';

                    SELECT @sql = @sql + 'DROP FUNCTION ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ';' + CHAR(13)
                    FROM sys.objects
                    WHERE type IN ('FN', 'FS', 'FT', 'IF', 'TF')
                      AND is_ms_shipped = 0
                      AND OBJECT_NAME(object_id) LIKE 'fn_clr%' OR OBJECT_NAME(object_id) LIKE 'fn_%';

                    SELECT @sql = @sql + 'DROP AGGREGATE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ';' + CHAR(13)
                    FROM sys.objects
                    WHERE type = 'AF'
                      AND is_ms_shipped = 0;

                    SELECT @sql = @sql + 'DROP TYPE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ';' + CHAR(13)
                    FROM sys.types
                    WHERE is_assembly_type = 1
                      AND is_user_defined = 1;

                    EXEC sp_executesql @sql;

                    DROP ASSEMBLY HartonomousSqlClrFunctions;
                END;
            ";
            ExecuteNonQuery(conn, sql);
        }

        static void CreateAssembly(SqlConnection conn, string dllPath, string permissionSet)
        {
            Console.WriteLine($"Creating assembly ({permissionSet})...");
            byte[] dllBytes = File.ReadAllBytes(dllPath);
            string hex = BitConverter.ToString(dllBytes).Replace("-", "");

            var sql = $@"
                CREATE ASSEMBLY HartonomousSqlClrFunctions
                FROM 0x{hex}
                WITH PERMISSION_SET = {permissionSet};
            ";
            ExecuteNonQuery(conn, sql);
        }

        static void RegisterFunctions(SqlConnection conn)
        {
            Console.WriteLine("Registering functions...");

            // Example - you'll need to add all your functions
            var functions = new[]
            {
                @"CREATE FUNCTION dbo.fn_GenerateWithAttention(
                    @modelId INT,
                    @inputAtomIds NVARCHAR(MAX),
                    @contextJson NVARCHAR(MAX),
                    @maxTokens INT,
                    @temperature FLOAT,
                    @topK INT,
                    @topP FLOAT,
                    @attentionHeads INT,
                    @tenantId INT
                ) RETURNS BIGINT
                AS EXTERNAL NAME HartonomousSqlClrFunctions.[SqlClrFunctions.AttentionGeneration].fn_GenerateWithAttention;",

                @"CREATE FUNCTION dbo.fn_CalculateComplexity(
                    @inputTokenCount INT,
                    @requiresMultiModal BIT,
                    @requiresToolUse BIT
                ) RETURNS INT
                AS EXTERNAL NAME HartonomousSqlClrFunctions.[SqlClrFunctions.AutonomousFunctions].fn_CalculateComplexity;",

                // Add all other functions here...
            };

            foreach (var sql in functions)
            {
                try
                {
                    ExecuteNonQuery(conn, sql);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: {ex.Message}");
                }
            }
        }

        static void RegisterAggregates(SqlConnection conn)
        {
            Console.WriteLine("Registering aggregates...");

            var aggregates = new[]
            {
                @"CREATE AGGREGATE dbo.VectorCentroid(@vectorJson NVARCHAR(MAX))
                RETURNS NVARCHAR(MAX)
                EXTERNAL NAME HartonomousSqlClrFunctions.[SqlClrFunctions.VectorCentroid];",

                @"CREATE AGGREGATE dbo.IsolationForestScore(@vectorJson NVARCHAR(MAX))
                RETURNS FLOAT
                EXTERNAL NAME HartonomousSqlClrFunctions.[SqlClrFunctions.IsolationForestScore];",

                // Add all 75+ aggregates here...
            };

            foreach (var sql in aggregates)
            {
                try
                {
                    ExecuteNonQuery(conn, sql);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: {ex.Message}");
                }
            }
        }

        static void RegisterUDTs(SqlConnection conn)
        {
            Console.WriteLine("Registering UDTs...");

            var udts = new[]
            {
                @"CREATE TYPE dbo.AtomicStream
                EXTERNAL NAME HartonomousSqlClrFunctions.[SqlClrFunctions.AtomicStream];",

                @"CREATE TYPE dbo.ComponentStream
                EXTERNAL NAME HartonomousSqlClrFunctions.[SqlClrFunctions.ComponentStream];",
            };

            foreach (var sql in udts)
            {
                try
                {
                    ExecuteNonQuery(conn, sql);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: {ex.Message}");
                }
            }
        }

        static void ExecuteNonQuery(SqlConnection conn, string sql)
        {
            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 300;
            cmd.ExecuteNonQuery();
        }
    }
}
```

---

## MIGRATION STEPS

### Step 1: Create New Projects (10 minutes)

```bash
cd D:\Repositories\Hartonomous\src

# Create Core library
dotnet new classlib -n Hartonomous.SqlClr.Core -f net48

# Create Functions library
dotnet new classlib -n Hartonomous.SqlClr.Functions -f net48
cd Hartonomous.SqlClr.Functions
dotnet add reference ../Hartonomous.SqlClr.Core/Hartonomous.SqlClr.Core.csproj
dotnet add package Microsoft.SqlServer.Types --version 160.1000.6
cd ..

# Create Deployment tool
dotnet new console -n Hartonomous.SqlClr.Deployment -f net8.0
cd Hartonomous.SqlClr.Deployment
dotnet add package Microsoft.Data.SqlClient
cd ../..

# Add to solution
dotnet sln Hartonomous.sln add src/Hartonomous.SqlClr.Core/Hartonomous.SqlClr.Core.csproj
dotnet sln Hartonomous.sln add src/Hartonomous.SqlClr.Functions/Hartonomous.SqlClr.Functions.csproj
dotnet sln Hartonomous.sln add src/Hartonomous.SqlClr.Deployment/Hartonomous.SqlClr.Deployment.csproj
```

### Step 2: Migrate Core Utilities (30 minutes)

**Create SimpleJsonBuilder.cs** (code above)

**Copy and clean VectorMath.cs**:
```bash
cp src/SqlClr/Core/VectorMath.cs src/Hartonomous.SqlClr.Core/Math/VectorMath.cs
```

Edit to remove SIMD (save for later):
```csharp
// Remove: using System.Runtime.Intrinsics;
// Remove: using System.Runtime.Intrinsics.X86;
// Keep only scalar implementations
```

**Copy VectorUtilities.cs**:
```bash
cp src/SqlClr/Core/VectorUtilities.cs src/Hartonomous.SqlClr.Core/Serialization/VectorParser.cs
```

Edit to use SimpleJsonBuilder instead of System.Text.Json.

**Copy LandmarkProjection.cs**:
```bash
cp src/SqlClr/Core/LandmarkProjection.cs src/Hartonomous.SqlClr.Core/Math/
```

### Step 3: Migrate Functions One by One (2-3 hours)

**Start with simplest**:

1. **VectorCentroid.cs** (simple aggregate):
   - Copy to `Aggregates/Advanced/VectorCentroid.cs`
   - Change namespace to `Hartonomous.SqlClr.Functions.Aggregates.Advanced`
   - Replace JSON calls with `SimpleJsonBuilder`
   - Build, fix errors
   - Test

2. **AttentionGeneration.cs** (complex function):
   - Copy to `Functions/Generation/AttentionGeneration.cs`
   - Update namespace
   - Replace JSON serialization
   - Update imports to use `Hartonomous.SqlClr.Core`
   - Build, fix

3. Repeat for all 52 files, testing each one

### Step 4: Build and Verify (30 minutes)

```bash
cd src/Hartonomous.SqlClr.Functions
dotnet build -c Release

# Should output:
# Build succeeded.
#     0 Warning(s)
#     0 Error(s)
```

### Step 5: Deploy (30 minutes)

```bash
cd src/Hartonomous.SqlClr.Deployment
dotnet run -- localhost Hartonomous ../../Hartonomous.SqlClr.Functions/bin/Release/net48/Hartonomous.SqlClr.Functions.dll SAFE
```

**Test in SQL**:
```sql
-- Test simple function
SELECT dbo.fn_CalculateComplexity(1000, 0, 0);

-- Test aggregate
SELECT dbo.VectorCentroid('[1,2,3]') FROM (VALUES (1)) AS t(x);

-- Test UDT
DECLARE @stream dbo.AtomicStream;
SET @stream = dbo.AtomicStream::Create(...);
SELECT @stream.StreamId;
```

### Step 6: Delete Old Project (5 minutes)

```bash
# After verifying everything works
rm -rf src/SqlClr
dotnet sln Hartonomous.sln remove src/SqlClr/SqlClrFunctions.csproj
```

---

## TESTING CHECKLIST

After migration, verify:

- [ ] All 75+ aggregates registered
- [ ] All functions callable from SQL
- [ ] AtomicStream UDT works
- [ ] ComponentStream UDT works
- [ ] AttentionGeneration function works
- [ ] No version conflicts in build output
- [ ] Assembly loads in SQL Server
- [ ] SAFE permission set (or UNSAFE if needed for SIMD later)

---

## BENEFITS

**Before (Old SqlClr project)**:
- 6 NuGet packages
- Version conflicts (System.Memory vs System.Text.Json)
- Old .csproj format
- Flat structure (50+ files in root)
- Build warnings
- Deployment fails

**After (New 3-project structure)**:
- 1 NuGet package (Microsoft.SqlServer.Types)
- Zero version conflicts
- Modern SDK-style project
- Organized folder structure
- Zero warnings
- Deployment succeeds

---

## TOTAL TIME ESTIMATE

| Task | Time |
|------|------|
| Create 3 projects | 10 min |
| Migrate Core utilities | 30 min |
| Create SimpleJsonBuilder | 20 min |
| Migrate 52 files | 2-3 hours |
| Build and fix errors | 30 min |
| Deploy and test | 30 min |
| Delete old project | 5 min |
| **TOTAL** | **4-5 hours** |

Compare to: **Days of NuGet debugging with no guarantee of success**.

---

## NEXT STEPS AFTER REBUILD

Once SqlClr is clean:

1. Add SIMD back (as optional, with scalar fallback)
2. Performance benchmark (verify 100x claim)
3. Add more aggregates as needed
4. Optimize hot paths

But first: **Get it working cleanly.**

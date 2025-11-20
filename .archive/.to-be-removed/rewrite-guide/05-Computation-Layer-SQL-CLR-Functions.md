# 05 - Computation Layer: The O(K) Refinement in SQL CLR

While the T-SQL layer performs the scalable `O(log N)` search, the procedural and computationally complex refinement work is delegated to C# functions running inside the SQL CLR. These C# source files are located in `src/Hartonomous.Database/CLR/` and are compiled directly by the `Hartonomous.Database.sqlproj` file.

This hybrid approach allows the database to do what it's best at (indexed lookups) and the CLR to do what it's best at: complex, stateful, procedural logic.

## 1. The Role of the CLR Layer: O(K) Processing

The CLR's primary role is to execute the `O(K)` part of the `O(log N) + O(K)` model. After T-SQL provides a small set of `K` candidate atoms, the CLR performs sophisticated operations on that small set, such as:

-   **Procedural Generation Loops:** Implementing the iterative, token-by-token generation logic, as seen in `AttentionGeneration.cs`. This includes state management, context windowing, and sampling.
-   **Advanced Filtering and Pathfinding:** Executing the proprietary logic for trilateration or A* pathfinding on the candidate set to find the optimal result.
-   **Complex Vector Math:** Performing vector projections and other calculations required for the custom attention mechanism.
-   **Interoperability:** Parsing complex binary formats (like GGUF model files) during ingestion.

## 2. Core Innovation: Queryable AI via CLR

A key innovation is the concept of **Queryable AI**. The CLR functions do not load monolithic model files. Instead, they treat the model's parameters as data to be queried from the database itself. The `AttentionGeneration.cs` file provides the canonical example: to compute attention, it executes a T-SQL query against `dbo.TensorAtoms` to retrieve the specific weights it needs, which are stored as `GEOMETRY` data. This makes the AI's internal state queryable and granular.

## 3. Key CLR Components

-   **`AttentionGeneration.cs`**: Implements the core `O(K)` iterative generation loop.
-   **`Core/VectorMath.cs`**: Provides SIMD-accelerated functions for vector operations used in the `O(K)` processing.
-   **`ModelParsers/*`**: A suite of classes for parsing model formats during ingestion.

## 4. Security Best Practices

Deploying code into the database process requires strict adherence to security best practices to protect the stability and integrity of the SQL Server instance.

-   **Principle of Least Privilege:** Assemblies should always be deployed with the most restrictive permission set possible. The default should be **`SAFE`**, which allows for computation and internal data access only.
-   **Escalate Permissions Carefully:** Only escalate to **`EXTERNAL_ACCESS`** (for network or file access) or **`UNSAFE`** (for unmanaged code or specific in-memory operations) when absolutely necessary. The out-of-process GPU worker communication, for example, would require `EXTERNAL_ACCESS`. The use of `UNSAFE` should be considered extremely high-risk and subject to rigorous code review.
-   **`clr strict security`:** Modern SQL Server versions have the `clr strict security` option enabled by default. This requires that **all** assemblies, regardless of their permission set, be signed with a certificate or an asymmetric key that has a corresponding login with the `UNSAFE ASSEMBLY` permission in the master database. This is a critical deployment prerequisite.

## 5. Critical Issue: Dependency and Compatibility

The `Hartonomous.Database.sqlproj` file reveals a critical stability and deployment risk. The CLR component has direct DLL references to libraries that are incompatible with the strict SQL Server CLR host.

-   **High-Risk Dependencies:** The references to `System.Collections.Immutable.dll` and `System.Reflection.Metadata.dll` are particularly problematic.
-   **The .NET Standard Problem:** While the .NET Framework itself supports .NET Standard 2.0, the SQL CLR host does **not**. The CLR host maintains a strict, limited list of supported .NET Framework libraries. When an assembly references a library that is not on this list (like the modern, NuGet-based `System.Collections.Immutable`), the CLR host cannot find it and will fail to load the parent assembly. This is a common and difficult-to-diagnose failure mode for SQL CLR.
-   **Impact:** This will cause the `CREATE ASSEMBLY` statement to fail in any clean SQL Server environment. The CLR assembly, as currently configured, is not reliably deployable.
-   **Resolution:** To create a stable system, all code relying on these incompatible dependencies **must be refactored**. The functionality must either be rewritten to remove the dependencies or be moved into an out-of-process worker service, as described in the `08-Advanced-Optimizations-Optional-GPU.md` guide.

This dependency issue is a top-priority technical debt item that must be addressed in the rewrite. The next document will explore the provenance graph.

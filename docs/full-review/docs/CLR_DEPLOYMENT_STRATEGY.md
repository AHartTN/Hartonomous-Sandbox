# docs/CLR_DEPLOYMENT_STRATEGY.md

## Purpose and Context

- Describes dual SQL CLR deployment model supporting SAFE (cloud/Azure SQL MI) and UNSAFE (on-prem GPU-enabled) assemblies for Hartonomous workloads.
- Covers security prerequisites (CLR strict security, trusted assemblies, signing) and outlines solution structure separating shared types, safe CPU operations, and unsafe GPU/filestream capabilities.

## Key Details

- SAFE assembly provides AVX2 SIMD cosine similarity and other CPU-only operations compliant with Azure SQL MI limitations; includes sample implementation and deployment T-SQL.
- UNSAFE assembly enables CUDA/cuBLAS interop, SqlFileStream ingestion, and other P/Invoke features requiring on-prem SQL Server 2025; code samples show GPU dot product and zero-copy ingestion routines.
- Provides deployment scripts for both environments, including certificate/asymmetric key setup, trusted assembly registration, DLL placement, and function/procedure wiring.
- Benchmark tables quantify performance gains (CPU SIMD 15x, GPU 100x) and ingestion efficiencies; testing strategy includes unit/integration tests verifying accuracy and throughput.

## Potential Risks / Follow-ups

- UNSAFE deployment depends on specific CUDA runtime versions and copying native DLLs into SQL Server bin directory; requires careful change control.
- Documentation assumes certain project layout and code artifacts exist; verify repository alignment before relying on instructions.
- GPU fallback, FILESTREAM rollback, and monitoring items are flagged as pending in production readiness checklist—ensure completion prior to release.

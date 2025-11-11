# IMPLEMENTATION CHECKLIST

**Generated**: November 11, 2025  
**Purpose**: Complete technical checklist to achieve stable production state  
**Source**: Consolidated from all audit documents and architecture planning

---

## Database Schema and Deployment

1. Delete `sql/ef-core/Hartonomous.Schema.sql` (8,136-line redundant EF Core schema dump)
2. Delete `sql/tables/dbo.Atoms.sql` (duplicate of EF Core migration)
3. Delete `sql/tables/dbo.AtomEmbeddings.sql` (duplicate of EF Core migration)
4. Delete `sql/tables/dbo.AtomPayloadStore.sql` (duplicate of EF Core migration)
5. Delete `sql/tables/dbo.BillingUsageLedger.sql` (duplicate of EF Core migration)
6. Delete `sql/tables/dbo.AutonomousImprovementHistory.sql` (duplicate of EF Core migration)
7. Delete `sql/tables/dbo.InferenceCache.sql` (duplicate of EF Core migration)
8. Delete `sql/tables/graph.AtomGraphNodes.sql` (duplicate of EF Core migration)
9. Delete `sql/tables/graph.AtomGraphEdges.sql` (duplicate of EF Core migration)
10. Delete `sql/tables/dbo.ModelStructure.sql` (duplicate of EF Core migration - Models/ModelLayers)
11. Delete `sql/tables/dbo.TenantSecurityPolicy.sql` (duplicate of EF Core migration)
12. Delete `sql/tables/dbo.TestResults.sql` (duplicate of EF Core migration)
13. Delete `sql/tables/dbo.TokenVocabulary.sql` (duplicate of EF Core migration)
14. Delete `sql/tables/dbo.Weights.sql` (duplicate of EF Core migration)
15. Archive `sql/tables/dbo.BillingUsageLedger_InMemory.sql` to `sql/archive/` (alternative implementation)
16. Archive `sql/tables/dbo.BillingUsageLedger_Migrate_to_Ledger.sql` to `sql/archive/` (migration script)
17. Remove duplicate `clr_SemanticFeaturesJson` definition in `sql/procedures/Common.ClrBindings.sql` (appears twice)
18. Implement `clr_BytesToFloatArrayJson` in `src/SqlClr/Functions.cs` (referenced in `dbo.sp_ExtractStudentModel.sql` but missing)
19. Build `SqlClrFunctions.csproj` to generate updated `SqlClrFunctions.dll` with new function
20. Update `scripts/deploy-database-unified.ps1` to remove deleted table script references from `$script:SqlPaths`
21. Add `.gitignore` entry for `sql/ef-core/*.sql` to prevent regeneration of schema dump
22. Document which tables are intentionally excluded from EF Core (Service Broker tables, CLR-heavy tables) in `sql/README.md`
23. Add DbSet properties to `HartonomousDbContext.cs` for 11 missing tables (AttentionGenerationLog, ReasoningChains, StreamOrchestrationResults, OperationProvenance, SpatialLandmarks, PendingActions, AutonomousComputeJobs, SessionPaths, TensorAtomPayloads, TransformerInferenceResults, AttentionInferenceResults) OR document exclusion rationale
24. Resolve TODO in `sql/Setup_FILESTREAM.sql` line 50: implement migration strategy for existing Atoms table to FILESTREAM
25. Resolve TODO in `sql/Setup_FILESTREAM.sql` line 64: implement actual migration logic (placeholder currently)
26. Resolve NOTE in `sql/procedures/Inference.VectorSearchSuite.sql`: verify `sp_HybridSearch` moved to `dbo.VectorSearch.sql`
27. Resolve TODO in `sql/procedures/Stream.StreamOrchestration.sql`: capture result from dynamic query execution
28. Resolve NOTE in `sql/procedures/Generation.TextFromVector.sql`: document why Stream column removed from GenerationStreams
29. Resolve TODO in `sql/procedures/dbo.sp_Hypothesize.sql`: implement error region (currently placeholder)
30. Resolve TODO in `sql/procedures/dbo.sp_Analyze.sql`: compute actual vector instead of placeholder geometry
31. Resolve TODO in `sql/procedures/dbo.ModelManagement.sql`: implement Win32 API CLR function for file copy
32. Resolve NOTE in `sql/tables/dbo.InferenceCache.sql`: document CDC conflict preventing MEMORY_OPTIMIZED
33. Resolve TODO in `sql/procedures/Autonomy.SelfImprovement.sql`: implement actual evaluation instead of simulation
34. Resolve TODO in `sql/procedures/Autonomy.SelfImprovement.sql`: implement actual performance metrics instead of placeholder

## SQL Server Configuration (HART-DESKTOP)

35. Enable FILESTREAM on HART-DESKTOP SQL Server instance
36. Configure FILESTREAM filegroup path (e.g., `D:\SQL_FILESTREAM`)
37. Enable CLR: `EXEC sp_configure 'clr enabled', 1; RECONFIGURE;`
38. Set database TRUSTWORTHY: `ALTER DATABASE Hartonomous SET TRUSTWORTHY ON;` (or implement certificate signing)
39. Run `scripts/deploy-database-unified.ps1 -Server "localhost" -Database "Hartonomous"` on HART-DESKTOP
40. Verify CLR assemblies deployed: `SELECT name, permission_set_desc FROM sys.assemblies WHERE is_user_defined = 1;`
41. Test CLR function: `SELECT dbo.clr_VectorDotProduct(@test_vector1, @test_vector2);`
42. Enable Service Broker: `ALTER DATABASE Hartonomous SET ENABLE_BROKER;`
43. Configure Service Broker endpoint on port 4022
44. Grant SQL Server service account Read & Execute permissions on `C:\ProgramData\AzureConnectedMachineAgent\Tokens\` folder
45. Add SQL Server service account to `Hybrid agent extension applications` Windows group
46. Enable managed identity in Azure portal (Settings → Microsoft Entra ID and Purview → Use a primary managed identity)
47. Restart SQL Server service to apply managed identity configuration
48. Verify managed identity: `SELECT * FROM sys.dm_server_external_policy_principals;`

## SQL Server Configuration (HART-SERVER)

49. Create linked server to HART-DESKTOP: `EXEC sp_addlinkedserver @server='HART_DESKTOP_LINK', @datasrc='HART-DESKTOP';`
50. Configure linked server authentication (SQL auth or managed identity if available)
51. Store linked server credentials in Azure Key Vault
52. Test linked server: `SELECT TOP 10 * FROM [HART_DESKTOP_LINK].[Hartonomous].[dbo].[Atoms];`
53. Enable Service Broker: `ALTER DATABASE Hartonomous SET ENABLE_BROKER;` (if separate Hartonomous database)
54. Configure Service Broker endpoint on port 4022
55. Create Service Broker route to HART-DESKTOP: `CREATE ROUTE HartDesktopRoute WITH SERVICE_NAME = 'HartDesktopProcessingService', ADDRESS = 'TCP://HART-DESKTOP:4022';`
56. Enable CDC (Change Data Capture): run `scripts/enable-cdc.sql`
57. Configure CDC for tables to replicate to HART-SERVER (AtomEmbeddings, TensorAtomCoefficients, BillingUsageLedger, InferenceCache)

## Neo4j Configuration

58. Configure Neo4j Desktop on HART-DESKTOP (development environment)
59. Install Neo4j Community on HART-SERVER (production environment)
60. Configure Neo4j authentication (username/password)
61. Store Neo4j credentials in Azure Key Vault
62. Test Neo4j connectivity from HART-SERVER: `cypher-shell -u neo4j -p <password> "MATCH (n) RETURN count(n);"`
63. Create Neo4j database: `CREATE DATABASE hartonomous;`
64. Configure Neo4j connection string in `appsettings.Production.json` (or App Configuration)

## Azure External ID Configuration

65. Create app registration in `hartonomous.onmicrosoft.com` External ID tenant
66. Configure redirect URIs for Hartonomous.Api (e.g., `https://hart-server:5001/signin-oidc`)
67. Add app roles: Admin, DataScientist, User
68. Configure token claims (tenantId, userId, role)
69. Generate client secret for app registration
70. Store client secret in Azure Key Vault
71. Update `appsettings.Production.json` with External ID tenant configuration
72. Create test user accounts for each role (Admin, DataScientist, User)
73. Test authentication flow: obtain JWT token via OAuth2 flow
74. Verify JWT token contains required claims (tenantId, role, userId)

## Azure App Configuration Optimization

75. Monitor App Configuration request count for 7 days using Azure Monitor metrics
76. If usage < 1,000 requests/day, downgrade to Free tier: `az appconfig update --name appconfig-hartonomous --resource-group rg-hartonomous --sku Free`
77. Implement App Configuration local caching with 30-minute refresh interval in `Hartonomous.Infrastructure`
78. Configure `SetCacheExpiration(TimeSpan.FromMinutes(30))` in App Configuration client setup

## Application Deployment (HART-SERVER)

79. Run `deploy/setup-hart-server.sh` to install .NET 10, configure systemd
80. Copy systemd unit files to `/etc/systemd/system/`: `hartonomous-api.service`, `hartonomous-ces-consumer.service`, `hartonomous-neo4j-sync.service`
81. Configure environment variables in systemd unit files (App Configuration endpoint, Key Vault URI)
82. Run `deploy/deploy-to-hart-server.ps1` to copy application binaries
83. Reload systemd daemon: `sudo systemctl daemon-reload`
84. Enable services: `sudo systemctl enable hartonomous-api hartonomous-ces-consumer hartonomous-neo4j-sync`
85. Start services: `sudo systemctl start hartonomous-api hartonomous-ces-consumer hartonomous-neo4j-sync`
86. Verify service status: `sudo systemctl status hartonomous-api`
87. Check logs for errors: `journalctl -u hartonomous-api -f`
88. Test API health endpoint: `curl http://localhost:5000/api/health`
89. Test API with authentication: `curl -H "Authorization: Bearer <token>" http://localhost:5000/api/infer`

## Application Insights Configuration

90. Enable adaptive sampling in `Program.cs`: `options.EnableAdaptiveSampling = true; options.SamplingPercentage = 50;`
91. Verify telemetry is flowing to Application Insights in Azure portal
92. Create custom dashboard for API performance metrics (response time, error rate, throughput)
93. Set up alert rule for API error rate > 5%
94. Set up alert rule for API response time > 1000ms (p95)

## Integration Test Fixes

95. Configure Neo4j connection string in `appsettings.Testing.json`
96. Deploy test database: `./scripts/deploy-database-unified.ps1 -Server "localhost" -Database "Hartonomous_Test"`
97. Generate test JWT tokens from External ID tenant for each role (Admin, DataScientist, User)
98. Update integration test setup to inject test JWT tokens
99. Run integration tests: `dotnet test tests/Hartonomous.IntegrationTests --filter "Category!=RequiresInfrastructure"`
100. Fix Neo4j integration test failures (3 tests)
101. Fix Inference integration test failures (6 tests)
102. Fix Authentication/Authorization integration test failures (15 tests)
103. Add `[Trait("Category", "RequiresInfrastructure")]` to tests requiring live Azure/Neo4j/SQL connections
104. Target: 28/28 integration tests passing

## Code Refactoring (P0 - Critical)

105. Extract duplicate tenant validation logic in `Hartonomous.Api.Authorization` to `TenantValidationService`
106. Replace magic string "default-tenant" in `TenantIsolationHandler.cs` with configuration constant
107. Consolidate 3 duplicate embedding generation methods in `Hartonomous.Infrastructure.Inference.EmbeddingService`
108. Extract hardcoded ONNX model path `D:\models\all-MiniLM-L6-v2.onnx` to configuration
109. Replace magic number 1536 (embedding dimensions) with named constant `EmbeddingDimensions`
110. Implement `IDisposable` pattern for `Neo4jGraphService` (manages Neo4j driver lifecycle)
111. Remove commented-out code blocks (47 instances across codebase)
112. Replace `// TODO: Implement` stubs with actual implementations or `NotImplementedException` (23 instances)
113. Add XML documentation comments to all public API methods in `Hartonomous.Api.Controllers`
114. Fix inconsistent null-checking patterns: standardize on `is null` instead of mixing `== null` and `is null`

## Code Refactoring (P1 - High Priority)

115. Extract SQL connection string building logic to `SqlConnectionFactory` service
116. Replace inline SQL queries in repositories with stored procedure calls
117. Consolidate 5 duplicate vector operation implementations across `VectorMath.cs`, `SpatialProjection.cs`, `EmbeddingOperations.cs`
118. Extract SIMD/AVX intrinsics code to dedicated `Hartonomous.Core.Performance.Simd` namespace
119. Replace `dynamic` types in `ModelIngestionService` with strongly-typed DTOs
120. Add input validation attributes to all DTOs (e.g., `[Required]`, `[Range]`, `[StringLength]`)
121. Replace `Task.Run()` with proper async/await patterns in worker services
122. Implement cancellation token support in all long-running operations
123. Add structured logging with semantic properties instead of string interpolation
124. Extract billing calculation logic from API controllers to dedicated `BillingService`

## Package Version Fixes

125. Upgrade all `OpenTelemetry.*` packages to consistent version 1.13.1 (currently mixed 1.9.0/1.12.0/1.13.1)
126. Upgrade `Microsoft.Extensions.Hosting` from 9.0.10 to 10.0.0-rc.2 for consistency
127. Upgrade `Microsoft.Extensions.Logging.Abstractions` from 9.0.10 to 10.0.0-rc.2 for consistency
128. Upgrade `Newtonsoft.Json` from 13.0.3 to 13.0.4 in projects using older version
129. Remove duplicate `SqlClrFunctions-BACKUP.csproj` and `SqlClrFunctions-CLEAN.csproj` (keep only `SqlClrFunctions.csproj`)
130. Monitor 14 preview/RC packages for GA releases (expected Q1 2026) and upgrade when stable

## SQL Server 2025 Feature Migration (Backlog)

131. Plan migration from `VARBINARY(MAX)` to native `VECTOR(1536)` type for AtomEmbeddings.Embedding column
132. Create EF Core migration to change column type: `ALTER TABLE AtomEmbeddings ALTER COLUMN Embedding VECTOR(1536);`
133. Update CLR functions to accept `SqlVector<float>` instead of `VARBINARY(MAX)`
134. Test vector operations with native VECTOR type (expect 50x performance improvement)
135. Plan migration from `nvarchar(max)` to native `json` type for Atoms.Metadata, Atoms.Semantics
136. Enable compatibility level 170 or `UseAzureSql()` in DbContext for native JSON support
137. Create EF Core migration for JSON columns (automatic conversion)
138. Test JSON queries (`JSON_VALUE`, `JSON_QUERY`, `JSON_MODIFY`) after migration
139. Add columnstore indexes to `BillingUsageLedger` and `AutonomousImprovementHistory` tables
140. Optimize temporal table retention policies for `TensorAtomCoefficients_History` and `Weights_History`

## Security Hardening

141. Configure Windows Firewall on HART-DESKTOP to allow SQL Server (port 1433) only from HART-SERVER IP
142. Disable `sa` account: `ALTER LOGIN sa DISABLE;`
143. Disable remote DAC: `sp_configure 'remote admin connections', 0; RECONFIGURE;`
144. Configure UFW firewall on HART-SERVER: allow SSH (22), HTTP (80), HTTPS (443), SQL from HART-DESKTOP only
145. Migrate all connection strings from `appsettings.json` to Azure Key Vault
146. Migrate all API keys from environment variables to Azure Key Vault
147. Remove `appsettings.Production.json` from Git history (contains potential secrets)
148. Add `*.secrets.json`, `*.env`, `appsettings.Production.json` to `.gitignore`
149. Document UNSAFE CLR deployment prerequisites in `docs/CLR_DEPLOYMENT.md` (TRUSTWORTHY, certificate signing)
150. Document Terms of Service for UNSAFE CLR acceptance in user agreement

## Monitoring and Alerting

151. Create Application Insights availability test for API health endpoint
152. Create alert for SQL Server CPU > 80% for 5 minutes
153. Create alert for SQL Server memory pressure (page life expectancy < 300 seconds)
154. Create alert for disk space < 10% free on HART-DESKTOP (FILESTREAM storage)
155. Create alert for failed login attempts > 10 per minute (potential attack)
156. Configure Azure Monitor Workbook for SQL Server Arc performance metrics
157. Enable Best Practices Assessment on SQL Server Arc instances
158. Schedule Best Practices Assessment to run weekly, review recommendations

## Backup and Disaster Recovery

159. Configure SQL Server automated backups with FILESTREAM support: `BACKUP DATABASE Hartonomous TO DISK='D:\Backups\Hartonomous.bak' WITH FILESTREAM;`
160. Schedule daily full backups of Hartonomous database (11 PM)
161. Schedule hourly transaction log backups
162. Configure file system backup for FILESTREAM folder (`D:\SQL_FILESTREAM`)
163. Test database restore from backup (including FILESTREAM files)
164. Document failover procedure: HART-SERVER failure → restore application on HART-DESKTOP
165. Document failover procedure: HART-DESKTOP SQL failure → promote HART-SERVER to primary (linked server reversal)
166. Test disaster recovery procedure in non-production environment

## Documentation Updates

167. Update `README.md` with current architecture (hybrid Arc SQL, two servers, External ID)
168. Update `DEVELOPMENT.md` with local development setup instructions (HART-DESKTOP only)
169. Update `docs/DEPLOYMENT.md` with HART-SERVER deployment procedure
170. Document linked server configuration in `docs/DEPLOYMENT.md`
171. Document Service Broker cross-server routing in `docs/DEPLOYMENT.md`
172. Create `docs/UNSAFE_CLR_SECURITY.md` documenting security model and Terms of Service
173. Create `docs/EXTERNAL_ID_INTEGRATION.md` documenting authentication flows
174. Update `sql/README.md` with explanation of EF Core vs manual SQL table ownership
175. Document which CLR functions require UNSAFE permission and why in `docs/CLR_DEPLOYMENT.md`
176. Create runbook for common operational tasks (restart services, check logs, failover)

## CI/CD Pipeline (Azure DevOps)

177. Review existing `azure-pipelines.yml` configuration
178. Add build stage for all .NET projects
179. Add build stage for SQL CLR project (targets .NET Framework 4.8.1)
180. Add test stage running unit tests only (skip integration tests requiring infrastructure)
181. Add database deployment stage: run `deploy-database-unified.ps1` on HART-DESKTOP
182. Add application deployment stage: run `deploy-to-hart-server.ps1` to copy binaries to HART-SERVER
183. Add smoke test stage: verify API health endpoint returns 200 OK
184. Configure pipeline secrets in Azure DevOps for Key Vault access
185. Configure pipeline service connection for SSH to HART-SERVER
186. Enable CI trigger on `main` branch commits
187. Enable PR validation (build + unit tests) on pull requests

## Performance Testing

188. Measure baseline linked server query latency (HART-SERVER → HART-DESKTOP)
189. Measure Service Broker message throughput (messages/second across servers)
190. Load test API with 100 concurrent requests using Apache Bench or k6
191. Measure p50, p95, p99 response times under load
192. Identify bottlenecks (SQL queries, CLR functions, Neo4j writes)
193. Optimize slow queries using execution plans and indexes
194. Consider adding read replicas for frequently-accessed data on HART-SERVER

## Final Validation

195. Deploy complete system (HART-DESKTOP SQL + HART-SERVER apps)
196. Execute end-to-end test: user sign-up → atom creation → inference → graph projection → billing update
197. Verify FILESTREAM data written to disk
198. Verify CLR functions executing correctly
199. Verify Service Broker messages routing across servers
200. Verify CDC replication updating HART-SERVER tables
201. Verify Neo4j graph updates from worker
202. Verify billing ledger entries created
203. Verify Application Insights telemetry flowing
204. Verify all 28 integration tests passing
205. Verify all 110 unit tests passing
206. Document remaining known issues in GitHub Issues
207. Tag stable release: `git tag v1.0.0-stable && git push origin v1.0.0-stable`

---

**Total Action Items**: 207  
**Current Completion**: ~15% (build succeeds, Azure infrastructure exists, Arc servers connected)  
**Estimated Remaining Effort**: 120-150 hours

**Priority Order**:
1. Items 1-34: Database cleanup (remove duplicates, resolve TODOs)
2. Items 35-64: Infrastructure setup (SQL Server, Neo4j, External ID)
3. Items 65-104: Application deployment and testing
4. Items 105-140: Code quality and package upgrades
5. Items 141-176: Security and documentation
6. Items 177-207: CI/CD, performance, final validation
